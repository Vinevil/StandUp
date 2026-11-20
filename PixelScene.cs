using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows.Forms;

namespace StandUp {
    public sealed class PixelScene:Control {
        public const string WallpaperFileFilter="图片 (*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff)|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff";
        const int WallpaperMaxSide=2048;
        public int Person,Scene,Task;
        public double StandAmount,Seconds;
        public Phase State;
        public Saying Quote;
        public bool Paused;
        public UiTheme Theme=UiTheme.White;
        Bitmap frame,wallpaper,scaledBackground;
        string wallpaperPath;
        int backgroundScene=-1;
        int lastPerson=-1,lastScene=-1;
        Size lastSize;
        double lastPose=-1,lastBob=-99;
        public PixelScene(){DoubleBuffered=true;SetStyle(ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);AccessibleName="像素人物与提醒场景";}
        static void Sprite(Graphics g,CharacterArt art,Rectangle source,float scale,float center,float floor,float alpha){
            if(alpha<=0)return;
            Rectangle dest=new Rectangle((int)(center-source.Width*scale/2),(int)(floor-source.Height*scale),(int)(source.Width*scale),(int)(source.Height*scale));
            using(ImageAttributes fade=new ImageAttributes()){
                ColorMatrix m=new ColorMatrix();m.Matrix33=alpha;fade.SetColorMatrix(m);
                g.DrawImage(art.Atlas,dest,source.X,source.Y,source.Width,source.Height,GraphicsUnit.Pixel,fade);
            }
        }
        static double Bob(bool walk,double t){return walk?Math.Round(Math.Sin(t*4)*2):0;}
        public bool TrySetWallpaper(string path,out string error){
            error=null;
            Bitmap next=null;
            string fullPath=null;
            if(!String.IsNullOrWhiteSpace(path)){
                try{
                    fullPath=Path.GetFullPath(path);
                    if(String.Equals(wallpaperPath,fullPath,StringComparison.OrdinalIgnoreCase)&&wallpaper!=null)return true;
                    next=ReadWallpaper(fullPath);
                }catch(FileNotFoundException){error="找不到这张壁纸，请重新选择。";}
                catch(DirectoryNotFoundException){error="找不到壁纸所在的文件夹。";}
                catch(UnauthorizedAccessException){error="无法读取这张壁纸，请检查文件权限。";}
                catch(SecurityException){error="无法读取这张壁纸，请检查文件权限。";}
                catch(InvalidDataException ex){error=ex.Message;}
                catch(IOException){error="无法打开这张壁纸，请重新选择。";}
                catch(ArgumentException){error="无法识别这张壁纸，请选择有效的图片。";}
                catch(NotSupportedException){error="无法识别这张壁纸，请选择有效的图片。";}
                catch(ExternalException){error="无法解码这张壁纸，请选择有效的图片。";}
                catch(OutOfMemoryException){error="无法解码这张壁纸，试试尺寸较小的 PNG 或 JPG。";}
                if(error!=null)return false;
            }
            if(wallpaper==null&&next==null)return true;
            if(wallpaper!=null)wallpaper.Dispose();
            wallpaper=next;
            wallpaperPath=fullPath;
            ReleaseFrame();
            Invalidate();
            return true;
        }
        static Bitmap ReadWallpaper(string path){
            using(FileStream file=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete)){
                if(file.Length>128L*1024*1024)throw new InvalidDataException("这张壁纸的文件太大，请选择 128 MB 以内的图片。");
                using(Image image=Image.FromStream(file,true,false)){
                    if((long)image.Width*image.Height>100000000)throw new InvalidDataException("这张壁纸的分辨率过高，请先缩小图片。");
                    float scale=Math.Min(1f,WallpaperMaxSide/(float)Math.Max(image.Width,image.Height));
                    Bitmap thumbnail=new Bitmap(Math.Max(1,(int)Math.Round(image.Width*scale)),Math.Max(1,(int)Math.Round(image.Height*scale)),PixelFormat.Format32bppPArgb);
                    try{
                        using(Graphics g=Graphics.FromImage(thumbnail)){
                            g.CompositingMode=CompositingMode.SourceCopy;
                            g.InterpolationMode=InterpolationMode.HighQualityBicubic;
                            g.PixelOffsetMode=PixelOffsetMode.HighQuality;
                            g.DrawImage(image,new Rectangle(0,0,thumbnail.Width,thumbnail.Height),0,0,image.Width,image.Height,GraphicsUnit.Pixel);
                        }
                        if(Array.IndexOf(image.PropertyIdList,0x0112)>=0){
                            byte[] value=image.GetPropertyItem(0x0112).Value;
                            int orientation=value.Length==0?1:value[0];
                            RotateFlipType[] rotations={RotateFlipType.RotateNoneFlipNone,RotateFlipType.RotateNoneFlipNone,
                                RotateFlipType.RotateNoneFlipX,RotateFlipType.Rotate180FlipNone,RotateFlipType.Rotate180FlipX,
                                RotateFlipType.Rotate90FlipX,RotateFlipType.Rotate90FlipNone,RotateFlipType.Rotate270FlipX,
                                RotateFlipType.Rotate270FlipNone};
                            if(orientation>=2&&orientation<=8)thumbnail.RotateFlip(rotations[orientation]);
                        }
                        return thumbnail;
                    }catch{thumbnail.Dispose();throw;}
                }
            }
        }
        public static Bitmap RenderWorld(int person,int scene,double stand,bool walk,double t){
            using(Bitmap background=ScaleBackground(ArtAssets.Background(scene),new Size(640,360)))
                return Compose(person,stand,walk,t,background);
        }
        static Bitmap ScaleBackground(Image background,Size size){
            Bitmap scaled=new Bitmap(size.Width,size.Height,PixelFormat.Format32bppPArgb);
            using(Graphics g=Graphics.FromImage(scaled)){
                g.Clear(Color.FromArgb(38,49,46));
                double k=Math.Max(size.Width/(double)background.Width,size.Height/(double)background.Height);
                float bw=(float)(background.Width*k),bh=(float)(background.Height*k);
                g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;
                g.DrawImage(background,new RectangleF((size.Width-bw)/2,(size.Height-bh)/2,bw,bh));
            }
            return scaled;
        }
        static Bitmap Compose(int person,double stand,bool walk,double t,Bitmap background){
            Bitmap bmp=new Bitmap(background.Width,background.Height,PixelFormat.Format32bppPArgb);
            PaintFrame(bmp,background,person,stand,walk,t);
            return bmp;
        }
        static void PaintFrame(Bitmap bmp,Bitmap background,int person,double stand,bool walk,double t){
            Size size=bmp.Size;
            using(Graphics g=Graphics.FromImage(bmp)){
                g.DrawImageUnscaled(background,0,0);
                g.SmoothingMode=SmoothingMode.AntiAlias;
                float center=size.Width*.76f,floor=size.Height*.958f+(float)Bob(walk,t)*size.Height/360f;
                CharacterArt art=ArtAssets.Character(person);float scale=Math.Min(size.Height*.65f/art.Standing.Height,size.Width*.38f/Math.Max(art.Standing.Width,art.Seated.Width));
                float shadowWidth=art.Seated.Width*scale*.75f;
                using(Brush b=new SolidBrush(Color.FromArgb(55,25,31,30)))g.FillEllipse(b,center-shadowWidth/2,floor-3,shadowWidth,Math.Max(4,size.Height*.03f));
                g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;
                float a=(float)Math.Max(0,Math.Min(1,stand));a=a*a*(3-2*a);
                // Registered seated/standing frames share their feet baseline and scale.
                Sprite(g,art,art.Seated,scale,center,floor-a*8,1-a);
                Sprite(g,art,art.Standing,scale,center,floor,a);
            }
        }
        public void PrepareFrame(){
            bool walk=State==Phase.Resting&&Task==1&&!Paused;double bob=Bob(walk,Seconds);
            if(Width<=0||Height<=0)return;
            if(scaledBackground==null||backgroundScene!=Scene||ClientSize!=lastSize){
                if(scaledBackground!=null)scaledBackground.Dispose();
                scaledBackground=ScaleBackground(wallpaper??ArtAssets.Background(Scene),ClientSize);
                backgroundScene=Scene;
            }
            if(frame==null||Person!=lastPerson||Scene!=lastScene||StandAmount!=lastPose||bob!=lastBob||ClientSize!=lastSize){
                if(frame==null||frame.Size!=ClientSize){
                    if(frame!=null)frame.Dispose();
                    frame=new Bitmap(ClientSize.Width,ClientSize.Height,PixelFormat.Format32bppPArgb);
                }
                PaintFrame(frame,scaledBackground,Person,StandAmount,walk,Seconds);
                lastPerson=Person;lastScene=Scene;lastPose=StandAmount;lastBob=bob;lastSize=ClientSize;
            }
        }
        protected override void OnPaint(PaintEventArgs e){
            base.OnPaint(e);Graphics g=e.Graphics;
            PrepareFrame();
            if(frame==null||Width<=0||Height<=0)return;
            g.SmoothingMode=SmoothingMode.AntiAlias;
            int cornerRadius=Math.Max(1,Math.Min(18,Math.Min(Width,Height)/12));
            using(GraphicsPath artBounds=SoftButton.Round(ClientRectangle,cornerRadius))
            using(TextureBrush image=new TextureBrush(frame,WrapMode.Clamp))g.FillPath(image,artBounds);
            g.PixelOffsetMode=PixelOffsetMode.Default;
            float s=Math.Min(Width/384f,Height/238f);
            bool speech=State==Phase.Alert||State==Phase.Resting||State==Phase.RestDone;
            if(speech&&Quote!=null){
                int x=(int)(12*s),y=(int)(14*s),w=(int)(Width*.53);
                int tx=x+(int)(12*s),tw=w-(int)(24*s);
                string source=JoinSource(Quote.Person,Quote.Source);
                TextFormatFlags wrap=TextFormatFlags.WordBreak|TextFormatFlags.TextBoxControl|TextFormatFlags.NoPadding|TextFormatFlags.NoPrefix;
                using(Font messageFont=new Font("Microsoft YaHei UI",12*s,FontStyle.Regular,GraphicsUnit.Pixel)){
                int textTop=(int)(39*s);
                int textLimit=(int)(139*s);
                string text=FitQuote(g,Quote.Text,messageFont,tw,textLimit,wrap);
                int textHeight=TextRenderer.MeasureText(g,text,messageFont,new Size(tw,0),wrap).Height;
                int sourceSpace=source.Length==0?0:(int)(19*s);
                int h=Math.Max((int)(92*s),textTop+textHeight+sourceSpace+(int)(13*s));
                Rectangle box=new Rectangle(x,y,w,h);
                using(GraphicsPath path=SoftButton.Round(box,Math.Max(1,(int)(10*s)))){
                    using(Brush shadow=new SolidBrush(Color.FromArgb(40,27,38,29))){
                        GraphicsState saved=g.Save();g.TranslateTransform(2*s,3*s);g.FillPath(shadow,path);g.Restore(saved);
                    }
                    using(Brush b=new SolidBrush(Theme.Background))g.FillPath(b,path);
                    using(Pen p=new Pen(Theme.Border,Math.Max(1,s)))g.DrawPath(p,path);
                }
                Point[] tail={new Point(box.Right-1,y+(int)(44*s)),new Point(box.Right+(int)(12*s),y+(int)(50*s)),new Point(box.Right-1,y+(int)(58*s))};
                using(Brush b=new SolidBrush(Theme.Background))g.FillPolygon(b,tail);
                string heading=State==Phase.Alert?"站起来！":State==Phase.Resting?"活动一会儿":"休息完成";
                using(Font f=new Font("Microsoft YaHei UI",18*s,FontStyle.Bold,GraphicsUnit.Pixel))TextRenderer.DrawText(g,heading,f,new Rectangle(tx,y+(int)(9*s),tw,(int)(27*s)),Theme.Alert,TextFormatFlags.NoPadding|TextFormatFlags.NoPrefix);
                TextRenderer.DrawText(g,text,messageFont,new Rectangle(tx,y+textTop,tw,textHeight),Theme.Text,wrap);
                if(sourceSpace>0)using(Font f=new Font("Microsoft YaHei UI",9*s,FontStyle.Regular,GraphicsUnit.Pixel))
                    TextRenderer.DrawText(g,source,f,new Rectangle(tx,box.Bottom-(int)(26*s),tw,sourceSpace),Theme.Muted,TextFormatFlags.EndEllipsis|TextFormatFlags.NoPadding|TextFormatFlags.NoPrefix|TextFormatFlags.VerticalCenter);
                }
            }
        }
        public static string JoinSource(string person,string source){
            if(String.IsNullOrWhiteSpace(person))return source??"";
            if(String.IsNullOrWhiteSpace(source))return person;
            return person+" · "+source;
        }
        static string FitQuote(Graphics g,string text,Font font,int width,int height,TextFormatFlags flags){
            int limit=Math.Min(768,text.Length);
            if(limit==text.Length&&TextRenderer.MeasureText(g,text,font,new Size(width,0),flags).Height<=height)return text;
            if(limit>0&&Char.IsHighSurrogate(text[limit-1]))limit--;
            int[] starts=StringInfo.ParseCombiningCharacters(text.Substring(0,limit));
            int low=0,high=starts.Length;
            while(low<high){
                int middle=(low+high+1)/2;
                int length=middle<starts.Length?starts[middle]:limit;
                string shortened=text.Substring(0,length).TrimEnd()+"…";
                if(TextRenderer.MeasureText(g,shortened,font,new Size(width,0),flags).Height<=height)low=middle;
                else high=middle-1;
            }
            return text.Substring(0,low<starts.Length?starts[low]:limit).TrimEnd()+"…";
        }
        void ReleaseFrame(){
            if(frame!=null){frame.Dispose();frame=null;}
            if(scaledBackground!=null){scaledBackground.Dispose();scaledBackground=null;}
        }
        protected override void Dispose(bool disposing){
            if(disposing){
                ReleaseFrame();
                if(wallpaper!=null){wallpaper.Dispose();wallpaper=null;}
            }
            base.Dispose(disposing);
        }
    }
}
