using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;

namespace StandUp {
    public sealed class Preferences {
        public int Person,Scene,Task,Theme,Minutes=30,RestMinutes=2,Width=420,Height=420,X=int.MinValue,Y=int.MinValue;
        public bool Top=true,Sound=false,Popup=true,StartCompact=false;
        public int CompactX=int.MinValue,CompactY=int.MinValue,DockSide;
        public string Wallpaper="";
        public static readonly string PathName=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"settings.xml");
        static int N(XElement root,string name,int fallback,int min,int max){int v;return int.TryParse((string)root.Element(name),out v)?Math.Max(min,Math.Min(max,v)):fallback;}
        public static Preferences Load(){
            Preferences p=new Preferences();
            try{
                if(!File.Exists(PathName))return p;
                using(XmlReader r=XmlReader.Create(PathName,new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null})){
                    XElement x=XDocument.Load(r).Root;if(x==null)return p;
                    int savedPerson;
                    if(int.TryParse((string)x.Element("Person"),out savedPerson))p.Person=CharacterLibrary.FromSavedId(savedPerson);
                    p.Scene=N(x,"Scene",0,0,SceneLibrary.Names.Length-1);p.Task=N(x,"Task",0,0,2);
                    p.Minutes=N(x,"Minutes",30,1,180);p.RestMinutes=N(x,"RestMinutes",2,1,30);
                    p.Width=N(x,"Width",420,360,1100);p.Height=N(x,"Height",420,386,900);
                    if(x.Element("Theme")==null&&x.Element("Height")!=null)p.Height=Math.Max(386,p.Height-40);
                    p.Theme=N(x,"Theme",0,0,UiTheme.Names.Length-1);
                    p.X=N(x,"X",int.MinValue,-32000,32000);p.Y=N(x,"Y",int.MinValue,-32000,32000);
                    p.Top=(string)x.Element("Top")!="false";p.Sound=(string)x.Element("Sound")=="true";
                    p.Popup=(string)x.Element("Popup")!="false";p.StartCompact=(string)x.Element("StartCompact")=="true";
                    p.CompactX=N(x,"CompactX",int.MinValue,-100000,100000);p.CompactY=N(x,"CompactY",int.MinValue,-100000,100000);
                    int side;
                    if(int.TryParse((string)x.Element("DockSide"),out side)&&Enum.IsDefined(typeof(DockEdge),side))p.DockSide=side;
                    p.Wallpaper=(string)x.Element("Wallpaper")??"";
                }
            }catch(IOException){}catch(UnauthorizedAccessException){}catch(XmlException){}return p;
        }
        public bool Save(){
            string temp=PathName+".tmp";
            try{
                new XDocument(new XElement("StandUp",new XElement("Person",CharacterLibrary.ToSavedId(Person)),new XElement("Scene",Scene),new XElement("Task",Task),
                    new XElement("Minutes",Minutes),new XElement("RestMinutes",RestMinutes),new XElement("Top",Top?"true":"false"),new XElement("Sound",Sound?"true":"false"),
                    new XElement("Popup",Popup?"true":"false"),new XElement("StartCompact",StartCompact?"true":"false"),new XElement("CompactX",CompactX),new XElement("CompactY",CompactY),new XElement("DockSide",DockSide),new XElement("Wallpaper",Wallpaper),
                    new XElement("Width",Width),new XElement("Height",Height),new XElement("X",X),new XElement("Y",Y),new XElement("Theme",Theme))).Save(temp);
                if(File.Exists(PathName))File.Replace(temp,PathName,null);else File.Move(temp,PathName);return true;
            }catch(IOException){}catch(UnauthorizedAccessException){}
            try{if(File.Exists(temp))File.Delete(temp);}catch{}return false;
        }
    }
    public class SoftButton:Button {
        public bool Primary;
        public bool LeftAligned;
        public Color PaintBackground=Color.Empty;
        public UiTheme Theme=UiTheme.White;
        public UiGlyph Glyph;
        bool hover;
        public SoftButton(){
            FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;UseVisualStyleBackColor=false;
            Cursor=Cursors.Hand;DoubleBuffered=true;Font=new Font("Microsoft YaHei UI",9f);
            SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.ResizeRedraw,true);
        }
        protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
        protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
        protected override void OnPaintBackground(PaintEventArgs e){
            Color color=PaintBackground.IsEmpty?(Parent==null?Theme.Background:Parent.BackColor):PaintBackground;
            using(Brush background=new SolidBrush(color))e.Graphics.FillRectangle(background,e.ClipRectangle);
        }
        protected override void OnPaint(PaintEventArgs e){
            OnPaintBackground(e);
            e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
            TextFormatFlags paintFlags=TextFormatFlags.PreserveGraphicsClipping|TextFormatFlags.PreserveGraphicsTranslateTransform;
            Color fill=Primary&&Enabled?Theme.Accent:hover&&Enabled?Theme.Hover:LeftAligned?Theme.Background:Theme.Surface;
            Color ink=Enabled?(Primary?Theme.AccentInk:Theme.Text):Theme.Muted;
            if(!Primary&&!LeftAligned&&Parent!=null&&Parent.BackColor==Theme.Surface&&PaintBackground.IsEmpty)
                fill=hover&&Enabled?Theme.Hover:Theme.Background;
            if(Primary&&hover&&Enabled)fill=Color.FromArgb((Theme.Accent.R*7+Theme.Surface.R)/8,(Theme.Accent.G*7+Theme.Surface.G)/8,(Theme.Accent.B*7+Theme.Surface.B)/8);
            if(Glyph==UiGlyph.Close&&hover&&Enabled)ink=Theme.Alert;
            int radius=Math.Max(4,(int)Math.Round(9*e.Graphics.DpiX/96f));
            using(GraphicsPath path=Round(new Rectangle(1,1,Width-2,Height-2),radius)){
                using(Brush b=new SolidBrush(fill))e.Graphics.FillPath(b,path);
                if(LeftAligned)using(Pen edge=new Pen(Theme.Border))e.Graphics.DrawPath(edge,path);
            }
            if(LeftAligned){
                int icon=Math.Min(Height/2,24), inset=Height/4;
                TextRenderer.DrawText(e.Graphics,Text,Font,new Rectangle(inset,0,Width-inset*2-icon,Height),ink,paintFlags|TextFormatFlags.SingleLine|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
                UiGlyphs.Draw(e.Graphics,UiGlyph.Chevron,new Rectangle(Width-inset-icon,(Height-icon)/2,icon,icon),Theme.Muted);
            }
            else if(Glyph==UiGlyph.None)
                TextRenderer.DrawText(e.Graphics,Text,Font,ClientRectangle,ink,paintFlags|TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPrefix);
            else{
                int size=Math.Min((int)Math.Round(19*e.Graphics.DpiX/96f),Math.Min(Width,Height)-6);
                if(size>0)UiGlyphs.Draw(e.Graphics,Glyph,new Rectangle((Width-size)/2,(Height-size)/2,size,size),ink);
            }
            if(Focused&&ShowFocusCues){
                using(GraphicsPath focus=Round(new Rectangle(4,4,Width-9,Height-9),radius-2))using(Pen p=new Pen(ink){DashStyle=DashStyle.Dot})e.Graphics.DrawPath(p,focus);
            }
        }
        public static GraphicsPath Round(Rectangle r,int radius){
            GraphicsPath p=new GraphicsPath();int d=Math.Min(radius*2,Math.Min(r.Width,r.Height));
            if(d<=1){if(r.Width>0&&r.Height>0)p.AddRectangle(r);return p;}
            p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;
        }
    }
    public sealed class MainWindow:Form {
        public readonly ReminderClock Clock=new ReminderClock();
        public readonly PixelScene SceneView=new PixelScene();
        readonly QuotePicker picker;
        readonly Preferences prefs;
        readonly bool preview;
        readonly float uiScale;
        readonly System.Windows.Forms.Timer timer=new System.Windows.Forms.Timer();
        readonly Stopwatch animation=Stopwatch.StartNew();
        readonly Label title=new Label(),digits=new Label(),status=new Label(),caption=new Label();
        readonly SoftButton close=new SoftButton(),minimize=new SoftButton(),settings=new SoftButton(),compactButton=new SoftButton(),main=new SoftButton(),pause=new SoftButton(),test=new SoftButton();
        readonly RoundedDrawer drawer=new RoundedDrawer();
        readonly ChoiceButton person=new ChoiceButton(),scene=new ChoiceButton(),task=new ChoiceButton(),themeChoice=new ChoiceButton();
        readonly NumberField minutes=new NumberField(),rest=new NumberField();
        readonly ThemeCheckBox top=new ThemeCheckBox(),sound=new ThemeCheckBox(),popupStyle=new ThemeCheckBox();
        readonly SoftButton apply=new SoftButton(),choosePicture=new SoftButton(),editQuotes=new SoftButton();
        readonly Label saving=new Label();
        Label restLabel;
        readonly NotifyIcon tray=new NotifyIcon();
        readonly ToolTip tip=new ToolTip();
        Icon appIcon;
        Icon trayIcon;
        CompactWindow compact;
        ReminderPopup reminder;
        SettingsWindow settingsWindow;
        UiTheme theme=UiTheme.White;
        readonly List<string> wallpapers=new List<string>();
        static readonly string WallpaperFolder=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"壁纸");
        bool initialized,compactMode,startInCompact;
        Size roundedSize;
        double riseStart;
        Phase oldState;
        bool oldPaused;
        const int MinimumHeight=386;
        internal static readonly uint RestoreMessage=RegisterWindowMessage("StandUp.Portable.Show.2026");
        [DllImport("user32.dll")]static extern bool ReleaseCapture();
        [DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr h,int msg,IntPtr w,IntPtr l);
        [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern uint RegisterWindowMessage(string name);
        [DllImport("user32.dll")]internal static extern bool PostMessage(IntPtr handle,uint message,IntPtr wParam,IntPtr lParam);
        public MainWindow(bool forPreview){
            preview=forPreview;prefs=forPreview?new Preferences():Preferences.Load();
            picker=preview?new QuotePicker(new Random(7)):new QuotePicker();
            if(!preview)picker.Load(QuotePicker.StatePath);
            DoubleBuffered=true;SetStyle(ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);
            using(Graphics dpi=Graphics.FromHwnd(IntPtr.Zero))uiScale=Math.Max(1,dpi.DpiX/96f);
            Text="站起来 · 桌面小憩";Font=new Font("Microsoft YaHei UI",9f);
            FormBorderStyle=FormBorderStyle.None;AutoScaleMode=AutoScaleMode.None;MinimumSize=ScaledSize(360,MinimumHeight);MaximumSize=ScaledSize(1100,1120);
            Size=ScaledSize(prefs.Width,prefs.Height);StartPosition=FormStartPosition.Manual;
            Rectangle area=Screen.PrimaryScreen.WorkingArea;
            Point desired=prefs.X==int.MinValue?new Point(area.Right-Width-30,area.Top+70):new Point(prefs.X,prefs.Y);
            Rectangle screen=Screen.FromPoint(desired).WorkingArea;
            Size=new Size(Math.Min(Width,screen.Width),Math.Min(Height,screen.Height));
            Location=new Point(Math.Max(screen.Left,Math.Min(desired.X,screen.Right-Width)),Math.Max(screen.Top,Math.Min(desired.Y,screen.Bottom-Height)));
            TopMost=!preview&&prefs.Top;appIcon=MakeIcon();Icon=appIcon;
            ShowInTaskbar=!preview;
            title.Text="站起来";title.Font=new Font(Font.FontFamily,11f,FontStyle.Bold);title.TextAlign=ContentAlignment.MiddleLeft;title.Cursor=Cursors.SizeAll;
            caption.Font=new Font(Font.FontFamily,8f);caption.ForeColor=Color.FromArgb(131,141,115);
            digits.Font=new Font("Consolas",32f,FontStyle.Bold);digits.ForeColor=Color.FromArgb(49,76,54);digits.TextAlign=ContentAlignment.MiddleLeft;
            status.TextAlign=ContentAlignment.MiddleLeft;status.Font=new Font(Font.FontFamily,8.5f);
            close.Glyph=UiGlyph.Close;close.AccessibleName="关闭程序";close.Click+=delegate{Close();};
            minimize.Glyph=UiGlyph.Minimize;minimize.AccessibleName="最小化到任务栏";minimize.Click+=delegate{MinimizeWindow();};
            settings.Glyph=UiGlyph.Settings;settings.AccessibleName="设置";settings.Click+=delegate{ToggleDrawer();};
            compactButton.Glyph=UiGlyph.Compact;compactButton.AccessibleName="精简模式";compactButton.Click+=delegate{EnterCompact();};
            main.Primary=true;main.Click+=delegate{MainAction();};pause.Click+=delegate{PauseOrSnooze();};
            test.Text="试一下";test.Click+=delegate{if(Clock.State==Phase.Working)Clock.TestAlert();else{SceneView.Quote=NextQuote();SceneView.Invalidate();}RefreshState();};
            tip.SetToolTip(test,"计时中可预览提醒；提醒或活动时可换一句，不影响计时");tip.SetToolTip(main,"现在开始活动，或开始下一轮计时");tip.SetToolTip(settings,"设置");
            title.MouseDown+=Drag;SceneView.MouseDown+=Drag;MouseDown+=Drag;
            tip.SetToolTip(compactButton,"切换为可贴边收起的小窗口");tip.SetToolTip(minimize,"收起到任务栏，计时继续");
            tip.SetToolTip(close,"关闭程序");tip.SetToolTip(title,"拖动标题移动，拖动边角缩放");
            foreach(Control c in new Control[]{title,caption,digits,status,close,minimize,settings,compactButton,main,pause,test,SceneView})Controls.Add(c);
            ContextMenuStrip pictureMenu=new ContextMenuStrip();pictureMenu.Items.Add("查看完整语录",null,delegate{ShowQuoteDetails();});pictureMenu.Opening+=delegate(object sender,System.ComponentModel.CancelEventArgs e){e.Cancel=SceneView.Quote==null;};SceneView.ContextMenuStrip=pictureMenu;
            BuildDrawer();ApplyTheme(UiTheme.FromIndex(prefs.Theme));
            SceneView.Person=prefs.Person;SceneView.Scene=prefs.Scene;SceneView.Task=prefs.Task;
            string wallpaperError;
            if(!string.IsNullOrEmpty(prefs.Wallpaper)&&!SceneView.TrySetWallpaper(ResolveWallpaper(prefs.Wallpaper),out wallpaperError))
                saving.Text="原壁纸无法读取，已显示内置场景";
            Clock.Due+=OnDue;Clock.StartWork(DateTime.UtcNow,TimeSpan.FromMinutes(prefs.Minutes));
            timer.Interval=1000;timer.Tick+=delegate{Tick();};if(!preview)timer.Start();
            if(!preview){
                trayIcon=AppMark.CreateIcon(SystemInformation.SmallIconSize);
                tray.Icon=trayIcon;tray.Text="站起来 · 久坐提醒";tray.Visible=true;
                ContextMenuStrip menu=new ContextMenuStrip();menu.Items.Add("显示主窗口",null,delegate{Restore();});menu.Items.Add("精简窗口",null,delegate{EnterCompact();});
                ToolStripItem minimizeItem=menu.Items.Add("收起到任务栏",null,delegate{MinimizeWindow();});
                menu.Opening+=delegate{
                    minimizeItem.Text=compactMode?"隐藏精简窗口":"收起到任务栏";
                    minimizeItem.Enabled=compactMode?compact.Visible:WindowState!=FormWindowState.Minimized;
                };
                menu.Items.Add("立即提醒",null,delegate{Clock.TestAlert();RefreshState();});
                menu.Items.Add("关闭程序",null,delegate{Close();});tray.ContextMenuStrip=menu;tray.DoubleClick+=delegate{Restore();};tray.BalloonTipClicked+=delegate{Restore();};
            }
            initialized=true;OnResize(EventArgs.Empty);RefreshState(true);
            startInCompact=!preview&&prefs.StartCompact;
            if(!startInCompact)SceneView.PrepareFrame();
        }
        protected override void SetVisibleCore(bool value){
            if(value&&startInCompact){
                startInCompact=false;
                // Keep an invisible top-level HWND for single-instance restore messages.
                // Application.Run's initial Show should present only the compact window.
                if(!IsHandleCreated)CreateHandle();
                EnterCompact(false);
                return;
            }
            base.SetVisibleCore(value);
        }
        public static Icon MakeIcon(){return AppMark.CreateIcon();}
        void Drag(object sender,MouseEventArgs e){if(e.Button!=MouseButtons.Left)return;ReleaseCapture();SendMessage(Handle,0xA1,(IntPtr)2,IntPtr.Zero);}
        int Px(int v){return (int)Math.Round(v*uiScale);}
        Size ScaledSize(int w,int h){return new Size(Px(w),Px(h));}
        void Place(Control c,int x,int y,int w,int h){c.SetBounds(Px(x),Px(y),Px(w),Px(h));}
        bool ShowingMain{get{return Visible&&WindowState==FormWindowState.Normal&&!compactMode;}}
        void Restore(){
            compactMode=false;
            if(compact!=null)compact.Hide();
            DismissReminder();
            RefreshState(true);Show();WindowState=FormWindowState.Normal;Activate();RefreshState();
        }
        void MinimizeWindow(){
            if(settingsWindow!=null)settingsWindow.Hide();
            DismissReminder();
            if(compactMode)compact.Hide();
            else WindowState=FormWindowState.Minimized;
            RefreshState();
        }
        void EnterCompact(){EnterCompact(true);}
        void EnterCompact(bool activate){
            if(settingsWindow!=null)settingsWindow.Hide();
            if(compact==null){
                compact=new CompactWindow(uiScale){TopMost=!preview&&prefs.Top,Icon=appIcon};
                compact.ApplyTheme(theme);
                if(prefs.CompactX!=int.MinValue&&prefs.CompactY!=int.MinValue)compact.RestorePlacement(new Point(prefs.CompactX,prefs.CompactY),prefs.DockSide);
                compact.ExpandRequested+=delegate{Restore();};compact.MinimizeRequested+=delegate{MinimizeWindow();};
                compact.ActionRequested+=delegate{if(Clock.State==Phase.Alert||Clock.State==Phase.RestDone)MainAction();else PauseOrSnooze();};
            }
            Rectangle previous=WindowState==FormWindowState.Normal?Bounds:RestoreBounds;
            compactMode=true;Hide();
            RefreshState();compact.ShowCompact(new Point(previous.Left+Px(18),previous.Top+Px(18)),activate);
        }
        protected override void OnHandleCreated(EventArgs e){
            base.OnHandleCreated(e);
            if(!preview)TaskbarIdentity.Bind(Handle);
        }
        protected override void WndProc(ref Message m){
            if(m.Msg==0x0002&&!preview)TaskbarIdentity.Unbind(m.HWnd); // WM_DESTROY, while the property store is still available
            if(RestoreMessage!=0&&m.Msg==(int)RestoreMessage){Restore();return;}
            if(m.Msg==0x84&&WindowState==FormWindowState.Normal){
                long v=m.LParam.ToInt64();Point pt=PointToClient(new Point((short)(v&65535),(short)((v>>16)&65535)));int edge=Px(7);
                bool left=pt.X<edge,right=pt.X>=Width-edge,up=pt.Y<edge,down=pt.Y>=Height-edge;
                int hit=left?(up?13:down?16:10):right?(up?14:down?17:11):up?12:down?15:0;
                if(hit!=0){m.Result=(IntPtr)hit;return;}
            }base.WndProc(ref m);
        }
        protected override CreateParams CreateParams{
            get{
                CreateParams parameters=base.CreateParams;
                parameters.Style|=0x00020000; // WS_MINIMIZEBOX keeps the borderless form restorable from the taskbar.
                return parameters;
            }
        }
        void BuildDrawer(){
            person.Items.AddRange(CharacterLibrary.Names);scene.Items.AddRange(SceneLibrary.Names);task.Items.AddRange(new[]{"起身活动","走动一下","休息片刻"});
            themeChoice.Items.AddRange(UiTheme.Names);
            person.AccessibleName="人物";scene.AccessibleName="场景";task.AccessibleName="任务";themeChoice.AccessibleName="主题";
            drawer.Controls.AddRange(new Control[]{person,scene,task,themeChoice});
            choosePicture.Text="选壁纸…";choosePicture.Click+=delegate{ChooseWallpaper();};drawer.Controls.Add(choosePicture);
            editQuotes.Text="编辑语录";editQuotes.Click+=delegate{EditQuotesFile();};drawer.Controls.Add(editQuotes);
            minutes.Minimum=1;minutes.Maximum=180;rest.Minimum=1;rest.Maximum=30;
            minutes.AccessibleName="提醒间隔";rest.AccessibleName="活动时长";drawer.Controls.AddRange(new Control[]{minutes,rest});
            top.Text="窗口置顶";sound.Text="提示音";drawer.Controls.Add(top);drawer.Controls.Add(sound);
            popupStyle.Text="收起时用小气泡提醒";drawer.Controls.Add(popupStyle);
            apply.Text="保存设置";apply.Primary=true;apply.Click+=delegate{ApplySettings();};drawer.Controls.Add(apply);
            saving.Font=new Font(Font.FontFamily,7.5f);saving.ForeColor=Color.FromArgb(98,115,94);saving.Text="时间单位：分钟";saving.AutoEllipsis=true;drawer.Controls.Add(saving);
            AddLabel("人物",12,12);AddLabel("场景",12,46);AddLabel("任务",12,80);AddLabel("主题",12,114);
            AddLabel("提醒间隔",12,150);restLabel=AddLabel("活动时长",174,150);
        }
        void ApplyTheme(UiTheme selected){
            theme=selected;
            BackColor=theme.Background;ForeColor=theme.Text;
            title.ForeColor=theme.Text;digits.ForeColor=theme.Text;status.ForeColor=theme.Text;caption.ForeColor=theme.Muted;
            drawer.BackColor=theme.Surface;drawer.ForeColor=theme.Text;saving.ForeColor=theme.Muted;
            foreach(SoftButton button in new SoftButton[]{close,minimize,settings,compactButton,main,pause,test,apply,choosePicture,editQuotes,person,scene,task,themeChoice}){
                button.Theme=theme;button.Invalidate();
            }
            foreach(ThemeCheckBox check in new[]{top,sound,popupStyle}){
                check.Theme=theme;check.BackColor=theme.Surface;check.Invalidate();
            }
            foreach(NumberField number in new[]{minutes,rest}){number.BackColor=theme.Surface;number.ApplyTheme(theme);}
            SceneView.BackColor=theme.Background;SceneView.Theme=theme;SceneView.Invalidate();
            if(compact!=null)compact.ApplyTheme(theme);
            if(reminder!=null)reminder.ApplyTheme(theme);
            if(settingsWindow!=null)settingsWindow.ApplyTheme(theme);
            Invalidate();
        }
        Label AddLabel(string text,int x,int y){Label l=new Label{Text=text,TextAlign=ContentAlignment.MiddleLeft};Place(l,x,y,66,23);drawer.Controls.Add(l);return l;}
        void LoadEditors(){
            person.SelectedIndex=prefs.Person;LoadWallpapers();task.SelectedIndex=prefs.Task;minutes.Value=prefs.Minutes;rest.Value=prefs.RestMinutes;
            top.Checked=prefs.Top;sound.Checked=prefs.Sound;popupStyle.Checked=prefs.Popup;themeChoice.SelectedIndex=prefs.Theme;
        }
        static string ResolveWallpaper(string path){return Path.IsPathRooted(path)?path:Path.Combine(AppDomain.CurrentDomain.BaseDirectory,path);}
        static string StoreWallpaper(string path){
            string appDir=AppDomain.CurrentDomain.BaseDirectory;
            return path.StartsWith(appDir,StringComparison.OrdinalIgnoreCase)?path.Substring(appDir.Length):path;
        }
        void LoadWallpapers(){
            scene.Items.Clear();scene.Items.AddRange(SceneLibrary.Names);wallpapers.Clear();
            try{
                if(Directory.Exists(WallpaperFolder)){
                    string[] files=Directory.GetFiles(WallpaperFolder);Array.Sort(files,StringComparer.OrdinalIgnoreCase);
                    foreach(string file in files){
                        string ext=Path.GetExtension(file).ToLowerInvariant();
                        if(ext==".png"||ext==".jpg"||ext==".jpeg"||ext==".bmp"||ext==".gif"||ext==".tif"||ext==".tiff")AddWallpaper(file);
                    }
                }
            }catch(IOException){}catch(UnauthorizedAccessException){}
            scene.SelectedIndex=prefs.Scene;
            if(!string.IsNullOrEmpty(prefs.Wallpaper))scene.SelectedIndex=AddWallpaper(ResolveWallpaper(prefs.Wallpaper));
        }
        int AddWallpaper(string file){
            int existing=wallpapers.FindIndex(delegate(string p){return string.Equals(p,file,StringComparison.OrdinalIgnoreCase);});
            if(existing>=0)return SceneLibrary.Names.Length+existing;
            wallpapers.Add(file);scene.Items.Add(Path.GetFileName(file));return scene.Items.Count-1;
        }
        void ChooseWallpaper(){
            string start=Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            try{Directory.CreateDirectory(WallpaperFolder);start=WallpaperFolder;}catch(IOException){}catch(UnauthorizedAccessException){}
            using(OpenFileDialog pickerDialog=new OpenFileDialog{Title="选择自定义壁纸",Filter=PixelScene.WallpaperFileFilter,InitialDirectory=start,RestoreDirectory=true}){
                if(pickerDialog.ShowDialog(settingsWindow)==DialogResult.OK){scene.SelectedIndex=AddWallpaper(pickerDialog.FileName);saving.Text="已选壁纸，点「保存设置」应用";}
            }
        }
        void EditQuotesFile(){
            try{
                string file=QuotePicker.SourcePath;
                if(!QuotePicker.EnsureSourceFile()){saving.Text="无法创建语录文件，请检查目录写入权限";return;}
                Process.Start(new ProcessStartInfo("notepad.exe","\""+file+"\""){UseShellExecute=true});
                saving.Text="每行一句，保存后下次抽取生效";
            }catch(Exception ex){saving.Text="无法打开语录文件："+ex.Message;}
        }
        void ToggleDrawer(){
            if(settingsWindow!=null&&settingsWindow.Visible){settingsWindow.Hide();return;}
            LoadEditors();
            if(settingsWindow==null){
                settingsWindow=new SettingsWindow(drawer,uiScale,Font,ArrangeSettings){TopMost=TopMost,Icon=appIcon};
                settingsWindow.ApplyTheme(theme);
            }
            settingsWindow.Present(this);
        }
        void ApplySettings(){
            int selection=scene.SelectedIndex;
            string chosenWallpaper=selection<SceneLibrary.Names.Length?null:wallpapers[selection-SceneLibrary.Names.Length];
            string error;
            if(!SceneView.TrySetWallpaper(chosenWallpaper,out error)){saving.Text=error;tip.SetToolTip(saving,error);return;}
            bool resetWork=Clock.State==Phase.Working&&prefs.Minutes!=(int)minutes.Value;
            prefs.Person=person.SelectedIndex;if(chosenWallpaper==null)prefs.Scene=selection;
            prefs.Wallpaper=chosenWallpaper==null?"":StoreWallpaper(chosenWallpaper);
            prefs.Task=task.SelectedIndex;prefs.Minutes=(int)minutes.Value;prefs.RestMinutes=(int)rest.Value;
            prefs.Top=top.Checked;prefs.Sound=sound.Checked;prefs.Popup=popupStyle.Checked;TopMost=!preview&&prefs.Top;
            prefs.Theme=themeChoice.SelectedIndex;
            UiTheme selectedTheme=UiTheme.FromIndex(prefs.Theme);
            if(selectedTheme!=theme)ApplyTheme(selectedTheme);
            if(compact!=null)compact.TopMost=TopMost;
            if(settingsWindow!=null)settingsWindow.TopMost=TopMost;
            SceneView.Person=prefs.Person;SceneView.Scene=prefs.Scene;SceneView.Task=prefs.Task;
            if(resetWork)Clock.StartWork(DateTime.UtcNow,TimeSpan.FromMinutes(prefs.Minutes));
            RefreshState();SceneView.Invalidate();SaveGeometry();
            saving.Text=preview?"预览模式":prefs.Save()?(resetWork?"已保存，已按新间隔计时":"已保存") : "无法写入设置，本次仍生效";
        }
        void Arrange(){
            if(!initialized)return;
            int w=(int)(ClientSize.Width/uiScale),contentH=(int)(ClientSize.Height/uiScale),m=18;
            SuspendLayout();
            try{
            Place(title,m,9,104,32);Place(compactButton,w-m-146,10,32,30);Place(settings,w-m-108,10,32,30);Place(minimize,w-m-70,10,32,30);Place(close,w-m-32,10,32,30);
            int sceneH=Math.Max(170,contentH-181);Place(SceneView,m,51,w-m*2,sceneH);
            int y=51+sceneH+10;Place(caption,m,y,w-36,18);Place(digits,m-2,y+14,178,51);Place(status,m+174,y+24,w-m*2-174,36);
            int by=y+67,bw=(w-m*2-16)/3;Place(main,m,by,bw,38);Place(pause,m+bw+8,by,bw,38);Place(test,m+(bw+8)*2,by,w-m-(m+(bw+8)*2),38);
            }finally{ResumeLayout(false);}
            Invalidate();
        }
        void ArrangeSettings(int dw){
            drawer.SuspendLayout();
            try{
                Place(person,77,9,dw-90,30);Place(scene,77,43,dw-177,30);Place(choosePicture,dw-94,43,81,30);
                Place(task,77,77,dw-90,30);Place(themeChoice,77,111,dw-90,30);
                int restEditor=dw/2+68;
                Place(minutes,77,147,77,29);Place(restLabel,dw/2+5,150,60,23);Place(rest,restEditor,147,dw-restEditor-13,29);
                Place(top,12,183,120,25);Place(sound,150,183,100,25);Place(popupStyle,12,211,185,25);Place(editQuotes,dw-98,210,85,27);
                Place(apply,12,241,dw-25,31);Place(saving,13,273,dw-26,17);
            }finally{drawer.ResumeLayout(false);}
        }
        protected override void OnResize(EventArgs e){
            base.OnResize(e);
            if(!initialized||WindowState!=FormWindowState.Normal)return;
            if(roundedSize!=ClientSize&&ClientSize.Width>0&&ClientSize.Height>0){
                roundedSize=ClientSize;
                using(GraphicsPath shape=SoftButton.Round(ClientRectangle,Px(14)))Region=new Region(shape);
            }
            Arrange();
            RefreshState();
        }
        protected override void OnPaint(PaintEventArgs e){
            base.OnPaint(e);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
            using(GraphicsPath outline=SoftButton.Round(new Rectangle(0,0,Width-1,Height-1),Px(14)))
                using(Pen p=new Pen(theme.Border))e.Graphics.DrawPath(p,outline);
        }
        void MainAction(){
            DismissReminder();
            if(Clock.State==Phase.Working||Clock.State==Phase.Alert){
                if(SceneView.Quote==null)SceneView.Quote=NextQuote();
                if(Clock.State==Phase.Working)riseStart=animation.Elapsed.TotalSeconds;
                Clock.StartRest(DateTime.UtcNow,TimeSpan.FromMinutes(prefs.RestMinutes));
            }else{Clock.StartWork(DateTime.UtcNow,TimeSpan.FromMinutes(prefs.Minutes));SceneView.Quote=null;}RefreshState();
        }
        void PauseOrSnooze(){
            if(Clock.State==Phase.Alert){Clock.Snooze(DateTime.UtcNow);SceneView.Quote=null;DismissReminder();}
            else Clock.TogglePause(DateTime.UtcNow);
            RefreshState();
        }
        void OnDue(){
            riseStart=animation.Elapsed.TotalSeconds;SceneView.Quote=NextQuote();
            if(!preview){
                if(prefs.Sound)System.Media.SystemSounds.Asterisk.Play();
                if(!ShowingMain&&prefs.Popup)ShowReminderPopup();
                else if(!ShowingMain)Restore();
                else if(!ContainsFocus&&Form.ActiveForm!=this)ShowReminderPopup();
            }
        }
        void ShowReminderPopup(){
            if(reminder==null){
                reminder=new ReminderPopup(uiScale){Icon=appIcon};
                reminder.ApplyTheme(theme);
                reminder.StartAction+=MainAction;reminder.SnoozeAction+=PauseOrSnooze;reminder.ViewAction+=Restore;
            }
            Screen screen=compact!=null&&compact.Visible?Screen.FromControl(compact):Screen.FromRectangle(WindowState==FormWindowState.Normal?Bounds:RestoreBounds);
            reminder.Present(SceneView.Quote,screen.WorkingArea);
        }
        void DismissReminder(){if(reminder!=null&&reminder.Visible)reminder.Hide();}
        void ShowQuoteDetails(){
            Saying quote=SceneView.Quote;if(quote==null)return;
            using(Form details=new Form{Text="完整语录",Icon=appIcon,Font=Font,StartPosition=FormStartPosition.CenterParent,Size=ScaledSize(390,245),ShowInTaskbar=false,MinimizeBox=false,MaximizeBox=false}){
                TextBox text=new TextBox{Text=quote.Text+Environment.NewLine+Environment.NewLine+PixelScene.JoinSource(quote.Person,quote.Source),BackColor=theme.Background,ForeColor=theme.Text,Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,Dock=DockStyle.Fill};
                details.Controls.Add(text);details.ShowDialog(this);
            }
        }
        Saying NextQuote(){Saying q=picker.Pick();if(!preview)picker.Save(QuotePicker.StatePath);return q;}
        void Tick(){
            Clock.Tick(DateTime.UtcNow);
            RefreshState();
        }
        public void RefreshState(bool prepareMain=false){
            Phase state=Clock.State;bool paused=Clock.Paused;
            double sinceRise=animation.Elapsed.TotalSeconds-riseStart;
            double pose=state==Phase.Working?0:ShowingMain?Math.Min(1,sinceRise/.9):1;
            bool redraw=SceneView.StandAmount!=pose||oldState!=state||oldPaused!=paused;
            if(!preview)SceneView.StandAmount=pose;
            SceneView.State=state;SceneView.Paused=paused;
            if(!preview){
                bool animating=ShowingMain&&state!=Phase.Working&&sinceRise<.9;
                bool timing=!paused&&(state==Phase.Working||state==Phase.Resting);
                int interval=animating?50:1000;
                if(timer.Interval!=interval)timer.Interval=interval;
                timer.Enabled=animating||timing;
            }
            TimeSpan remain=Clock.Remaining(DateTime.UtcNow);int sec=(int)Math.Ceiling(remain.TotalSeconds);
            string countdown=(sec/60).ToString("00")+":"+(sec%60).ToString("00");
            if(compact!=null&&compactMode)compact.UpdateTimer(state,paused,countdown);
            oldState=state;oldPaused=paused;
            if(!prepareMain&&!preview&&!ShowingMain)return;
            digits.Text=countdown;
            caption.Text=Clock.State==Phase.Working?(Clock.Paused?"已暂停 · 等你回来":"距离下一次起身"):(Clock.State==Phase.Alert?"到点了 · 给身体一点空隙":Clock.State==Phase.Resting?"活动倒计时":"休息完成 · 按需开始下一轮");
            status.Text=Clock.State==Phase.Working?"每 "+prefs.Minutes+" 分钟提醒\n活动 "+prefs.RestMinutes+" 分钟":Clock.State==Phase.Alert?new[]{"起身活动","走动一下","休息片刻"}[prefs.Task]+"\n确认后开始计时":Clock.State==Phase.Resting?new[]{"起身活动","走动一下","休息片刻"}[prefs.Task]+"\n按舒适的节奏来":"准备好了再继续";
            main.Text=Clock.State==Phase.Working?"现在活动":Clock.State==Phase.Alert?"开始活动":Clock.State==Phase.Resting?"结束活动":"下一轮";
            pause.Text=Clock.State==Phase.Alert?"稍后1分钟":Clock.Paused?"继续":"暂停";pause.Enabled=Clock.State!=Phase.RestDone;
            test.Enabled=true;test.Text=Clock.State==Phase.Working?"试一下":"换一句";SceneView.AccessibleDescription=caption.Text+" "+digits.Text+(SceneView.Quote==null?"":" "+SceneView.Quote.Text);
            if(redraw)SceneView.Invalidate();
        }
        void SaveGeometry(){
            Rectangle current=WindowState==FormWindowState.Normal?Bounds:RestoreBounds;
            if(current.Width>0&&current.Height>0){
                prefs.Width=(int)(current.Width/uiScale);prefs.Height=Math.Max(MinimumHeight,(int)(current.Height/uiScale));prefs.X=current.Left;prefs.Y=current.Top;
            }
            prefs.StartCompact=compactMode;
            if(compact!=null){Point location=compact.RememberedLocation;prefs.CompactX=location.X;prefs.CompactY=location.Y;prefs.DockSide=compact.DockSide;}
        }
        protected override void OnFormClosing(FormClosingEventArgs e){if(!preview){SaveGeometry();prefs.Save();picker.Save(QuotePicker.StatePath);}base.OnFormClosing(e);}
        protected override void Dispose(bool disposing){
            if(disposing&&!IsDisposed){timer.Stop();timer.Dispose();if(compact!=null)compact.Dispose();if(reminder!=null)reminder.Dispose();if(settingsWindow!=null)settingsWindow.Dispose();else drawer.Dispose();tray.Visible=false;if(tray.ContextMenuStrip!=null)tray.ContextMenuStrip.Dispose();tray.Dispose();if(trayIcon!=null)trayIcon.Dispose();if(SceneView.ContextMenuStrip!=null)SceneView.ContextMenuStrip.Dispose();tip.Dispose();if(appIcon!=null)appIcon.Dispose();}base.Dispose(disposing);
        }
        void PrepareAlertPreview(Saying quote){
            Clock.TestAlert();SceneView.Quote=quote;SceneView.StandAmount=1;RefreshState();
        }
        public void RenderPreview(string folder){
            Directory.CreateDirectory(folder);Show();timer.Stop();
            Saying previewQuote=Array.Find(Sayings.All,q=>q.Person=="每日鼓励");
            Size=ScaledSize(360,400);SceneView.Scene=3;
            for(int i=0;i<UiTheme.Names.Length;i++){
                ApplyTheme(UiTheme.FromIndex(i));
                CaptureImage(folder,"主窗口-"+UiTheme.Names[i]+".png");
            }
            ApplyTheme(UiTheme.White);
            foreach(int w in new[]{360,420,640}){
                Size=ScaledSize(w,478);Clock.StartWork(DateTime.UtcNow,TimeSpan.FromMinutes(30));SceneView.StandAmount=0;SceneView.Person=0;SceneView.Scene=0;SceneView.Quote=null;RefreshState();CaptureImage(folder,"工作-"+w+".png");
                PrepareAlertPreview(previewQuote);CaptureImage(folder,"提醒-"+w+".png");
            }
            Size=ScaledSize(420,478);SceneView.Person=1;SceneView.Scene=1;PrepareAlertPreview(previewQuote);CaptureImage(folder,"慢羊羊村长-湖边.png");
            SceneView.Scene=2;CaptureImage(folder,"慢羊羊村长-庭院.png");
            for(int sc=0;sc<SceneLibrary.Names.Length;sc++){
                SceneView.Scene=sc;SceneView.Person=0;SceneView.Quote=null;SceneView.StandAmount=0;Clock.StartWork(DateTime.UtcNow,TimeSpan.FromMinutes(30));RefreshState();CaptureImage(folder,"场景-"+SceneLibrary.Keys[sc]+".png");
                PrepareAlertPreview(previewQuote);CaptureImage(folder,"站起-"+SceneLibrary.Keys[sc]+".png");
            }
            ToggleDrawer();ApplyTheme(UiTheme.Black);themeChoice.SelectedIndex=1;CaptureForm(settingsWindow,folder,"设置-黑色.png");ToggleDrawer();
            ApplyTheme(UiTheme.White);
            Saying longest=previewQuote;foreach(Saying q in Sayings.All)if(q.Person=="每日鼓励"&&q.Text.Length>longest.Text.Length)longest=q;
            Size=ScaledSize(360,478);PrepareAlertPreview(longest);CaptureImage(folder,"励志语录-小窗口.png");
            Size=ScaledSize(420,478);CaptureImage(folder,"励志语录-新版.png");
            Size=ScaledSize(360,400);
            for(int p=0;p<CharacterLibrary.Names.Length;p++){
                SceneView.Person=p;SceneView.Scene=1;SceneView.Quote=null;SceneView.StandAmount=0;
                Clock.StartWork(DateTime.UtcNow,TimeSpan.FromMinutes(30));RefreshState();CaptureImage(folder,CharacterLibrary.Keys[p]+"-坐下.png");
                PrepareAlertPreview(previewQuote);CaptureImage(folder,CharacterLibrary.Keys[p]+"-站起.png");
            }
            Clock.StartWork(DateTime.UtcNow,TimeSpan.FromMinutes(30));SceneView.Quote=null;EnterCompact(false);CaptureForm(compact,folder,"精简模式.png");
            for(int i=0;i<UiTheme.Names.Length;i++){
                ApplyTheme(UiTheme.FromIndex(i));CaptureForm(compact,folder,"精简模式-"+UiTheme.Names[i]+".png");
            }
            ApplyTheme(UiTheme.Black);
            AnchorStyles taskbars=TaskbarEdges.OnScreen(Screen.FromControl(compact));
            foreach(DockEdge edge in CompactDock.Edges)if(CompactDock.IsAllowed(edge,taskbars)){
                compact.RestorePlacement(compact.RememberedLocation,(int)edge);compact.ShowCompact(compact.Location,false);CaptureForm(compact,folder,"贴边把手.png");break;
            }
            PrepareAlertPreview(previewQuote);ShowReminderPopup();CaptureForm(reminder,folder,"提醒气泡.png");Close();
        }
        public bool IsFirstFrameReady(){
            return !Visible&&digits.Text.Length>0&&caption.Text.Length>0&&status.Text.Length>0&&main.Text=="现在活动"&&pause.Text=="暂停";
        }
        public int CheckInterface(){
            int count=0;
            Action<bool,string> check=delegate(bool ok,string name){if(!ok)throw new Exception("UI test: "+name);count++;};
            Show();pause.PerformClick();check(Clock.Paused,"pause click");pause.PerformClick();check(!Clock.Paused,"resume click");
            test.PerformClick();check(Clock.State==Phase.Alert&&SceneView.Quote!=null,"test click and quote");
            string oldQuote=SceneView.Quote.Id;DateTime originalDeadline=Clock.Deadline;test.PerformClick();check(SceneView.Quote.Id!=oldQuote&&Clock.State==Phase.Alert&&Clock.Deadline==originalDeadline,"next quote preserves pending alert");
            pause.PerformClick();check(Clock.State==Phase.Working&&Clock.Duration==TimeSpan.FromMinutes(1),"snooze click");
            main.PerformClick();check(Clock.State==Phase.Resting,"start activity click");pause.PerformClick();check(Clock.Paused,"rest pause click");TimeSpan pausedRest=Clock.Remaining(DateTime.UtcNow);test.PerformClick();check(Clock.Paused&&Clock.State==Phase.Resting&&Clock.Remaining(DateTime.UtcNow)==pausedRest,"next quote preserves paused rest");main.PerformClick();check(Clock.State==Phase.Working&&!Clock.Paused,"end activity click");
            Size beforeSettings=Size;
            settings.PerformClick();check(settingsWindow.Visible&&drawer.Visible&&Size==beforeSettings,"settings open separately without resizing main");
            for(int i=0;i<60;i++){
                IntPtr wheel=(IntPtr)((i%2==0?-120:120)<<16);
                SendMessage(top.Handle,0x020A,wheel,IntPtr.Zero);
                SendMessage(SceneView.Handle,0x020A,wheel,IntPtr.Zero);
            }
            check(Size==beforeSettings&&settingsWindow.Visible,"scrolling does not resize main or close settings");
            using(Bitmap checkbox=new Bitmap(top.Width,top.Height)){
                top.DrawToBitmap(checkbox,top.ClientRectangle);
                check(checkbox.GetPixel(checkbox.Width-2,2).ToArgb()==theme.Surface.ToArgb(),"checkbox paints its entire background");
            }
            DateTime beforeTheme=Clock.Deadline;
            for(int i=UiTheme.Names.Length-1;i>=0;i--){
                themeChoice.SelectedIndex=i;apply.PerformClick();
                check(BackColor==UiTheme.FromIndex(i).Background&&SceneView.Theme==UiTheme.FromIndex(i)&&Clock.Deadline==beforeTheme,"theme applies without resetting timer");
                check(SelfTests.UsesApplicationIcons(this)&&SelfTests.UsesApplicationIcons(settingsWindow),"main and settings keep their native icons after a theme change");
            }
            person.SelectedIndex=1;scene.SelectedIndex=1;task.SelectedIndex=1;minutes.Value=17;rest.Value=3;apply.PerformClick();
            check(SceneView.Person==1&&SceneView.Scene==1&&SceneView.Task==1&&Clock.Duration==TimeSpan.FromMinutes(17),"apply settings");
            for(int p=0;p<CharacterLibrary.Names.Length;p++){
                person.SelectedIndex=p;apply.PerformClick();
                check(SceneView.Person==p,"animated companion selectable from settings");
            }
            test.PerformClick();check(SceneView.Quote!=null&&test.Text=="换一句","shared quotes available for switched character");
            for(int sc=0;sc<5;sc++){scene.SelectedIndex=sc;apply.PerformClick();check(SceneView.Scene==sc,"all new scenes apply");}
            settings.PerformClick();check(!settingsWindow.Visible&&Size==beforeSettings,"settings close without resizing main");
            foreach(int w in new[]{360,420,640}){
                Size=ScaledSize(w,MinimumHeight);Arrange();check(Height-main.Bottom<=Px(18)&&Height-main.Bottom>=Px(10),"compact lower margin");
                check(main.Right<pause.Left&&pause.Right<test.Left&&test.Right<Width,"action buttons separated");
                settings.PerformClick();check(saving.Bottom<=drawer.Height&&Size==ScaledSize(w,MinimumHeight),"settings fit without changing main size");check(minutes.Right<rest.Left&&rest.Right<drawer.Width,"numeric fields fit");settings.PerformClick();
            }
            Clock.StartWork(DateTime.UtcNow,TimeSpan.FromMinutes(30));
            DateTime compactDeadline=Clock.Deadline;
            Rectangle fullBounds=Bounds;
            EnterCompact(false);
            check(!Visible&&compact.Visible&&!compact.ShowInTaskbar,"compact hides the taskbar window");
            check(SelfTests.UsesApplicationIcons(compact),"compact uses application icons");
            compact.Reveal(false);
            Rectangle originalCompact=compact.Bounds;
            Screen display=Screen.FromRectangle(originalCompact);
            AnchorStyles taskbars=TaskbarEdges.OnScreen(display);
            foreach(DockEdge edge in CompactDock.Edges){
                compact.RestorePlacement(originalCompact.Location,(int)edge);
                compact.ShowCompact(originalCompact.Location,false);
                if(CompactDock.IsAllowed(edge,taskbars)){
                    bool vertical=edge==DockEdge.Left||edge==DockEdge.Right;
                    Size stripSize=vertical?new Size(Px(4),originalCompact.Height):new Size(originalCompact.Width,Px(4));
                    check(compact.IsTucked&&compact.Size==stripSize,"compact strip preserves its length on "+edge);
                    compact.Reveal(false);
                    check(!compact.IsTucked&&compact.Size==originalCompact.Size,"compact restores its size on "+edge);
                }else check(!compact.IsTucked&&compact.DockSide==0&&display.WorkingArea.Contains(compact.Bounds),"saved taskbar edge opens inside work area");
            }
            MinimizeWindow();
            check(!Visible&&!compact.Visible&&compactMode&&Clock.Deadline==compactDeadline,"hiding compact keeps the timer in the notification area");
            EnterCompact();check(!Visible&&compact.Visible,"compact can be reopened from the tray");
            Restore();check(ShowingMain&&!compact.Visible&&Bounds==fullBounds&&Clock.Deadline==compactDeadline,"expanding restores the original main window without resetting the clock");
            MinimizeWindow();check(Visible&&WindowState==FormWindowState.Minimized,"ordinary minimize keeps the main window visible to the shell");
            EnterCompact(false);check(!Visible&&compact.Visible,"compact can be entered from ordinary minimize");
            Clock.TestAlert();ShowReminderPopup();check(reminder.Visible&&!reminder.ShowInTaskbar,"reminder appears above compact");
            check(SelfTests.UsesApplicationIcons(reminder),"reminder uses application icons");
            PauseOrSnooze();check(!reminder.Visible&&Clock.State==Phase.Working,"snoozing dismisses popup and resumes timer");
            Restore();check(!compact.Visible&&ShowingMain,"restoring the main window from the tray hides compact");
            Close();return count;
        }
        void CaptureImage(string folder,string name){CaptureForm(this,folder,name);}
        static void CaptureForm(Form form,string folder,string name){
            form.PerformLayout();form.Refresh();Application.DoEvents();
            using(Bitmap image=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(image,form.ClientRectangle);image.Save(Path.Combine(folder,name));}
        }
    }
    static class Program {
        [STAThread]static int Main(string[] args){
            try{
                Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
                if(args.Length>1&&args[0]=="--export-icon"){AppMark.SaveIcon(args[1]);return 0;}
                if(args.Length>1&&args[0]=="--build-icon"){AppMark.SaveClockIcon(args[1]);return 0;}
                if(args.Length>0&&args[0]=="--self-test"){SelfTests.Run(args.Length>1?args[1]:"self-test.txt");return 0;}
                if(args.Length>0&&args[0]=="--render-preview"){using(MainWindow w=new MainWindow(true))w.RenderPreview(args.Length>1?args[1]:"preview");return 0;}
                bool created;using(Mutex gate=new Mutex(true,"Local\\StandUpPortableDesktopReminder",out created)){
                    if(!created){if(MainWindow.RestoreMessage!=0)MainWindow.PostMessage((IntPtr)0xffff,MainWindow.RestoreMessage,IntPtr.Zero,IntPtr.Zero);return 0;}
                    TaskbarIdentity.SetProcessId();
                    Application.Run(new MainWindow(false));
                }return 0;
            }catch(Exception ex){MessageBox.Show("启动遇到问题：\n"+ex.Message,"站起来",MessageBoxButtons.OK,MessageBoxIcon.Error);return 1;}
        }
    }
}
