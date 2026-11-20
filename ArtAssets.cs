using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace StandUp {
    public static class SceneLibrary {
        public static readonly string[] Names={"树下小憩","湖畔微风","庭院听风","紫禁城·金色余晖","喜马拉雅·雪山晨光"};
        public static readonly string[] Keys={"tree","lake","garden","forbidden-city","himalaya"};
    }
    public static class CharacterLibrary {
        public static readonly string[] Names={"猪猪侠","慢羊羊村长","橙留香"};
        public static readonly string[] Keys={"gg-bond","slow-goat","orange-hero-v16"};
        // The selection list can change independently of the IDs saved in settings.xml.
        static readonly int[] SavedIds={2,3,4};
        internal static int FromSavedId(int id){return Math.Max(0,Array.IndexOf(SavedIds,id));}
        internal static int ToSavedId(int index){return SavedIds[index];}
    }
    public sealed class CharacterArt {
        public readonly Bitmap Atlas;
        public readonly Rectangle Standing,Seated;
        public CharacterArt(Bitmap b){
            Atlas=b;int half=b.Width/2;
            Standing=Bounds(b,new Rectangle(0,0,half,b.Height));Seated=Bounds(b,new Rectangle(half,0,b.Width-half,b.Height));
        }
        static Rectangle Bounds(Bitmap b,Rectangle cell){
            int left=cell.Right,top=cell.Bottom,right=cell.Left,bottom=cell.Top;
            // Read the alpha channel in rows, without calling into GDI+ for each pixel.
            BitmapData pixels=b.LockBits(cell,ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
            byte[] row=new byte[cell.Width*4];
            try{
                for(int y=0;y<cell.Height;y++){
                    Marshal.Copy(IntPtr.Add(pixels.Scan0,y*pixels.Stride),row,0,row.Length);
                    for(int x=0;x<cell.Width;x++){
                        if(row[x*4+3]<=40)continue;
                        left=Math.Min(left,cell.X+x);top=Math.Min(top,cell.Y+y);
                        right=Math.Max(right,cell.X+x);bottom=Math.Max(bottom,cell.Y+y);
                    }
                }
            }finally{b.UnlockBits(pixels);}
            if(right<=left||bottom<=top)throw new InvalidDataException("人物图集缺少透明角色图层");
            return Rectangle.FromLTRB(left,top,right+1,bottom+1);
        }
    }
    public static class ArtAssets {
        static int activeScene=-1;
        static Bitmap activeBackground;
        static int activePerson=-1;
        static CharacterArt activeCharacter;
        static Bitmap ReadEmbedded(string key){
            using(Stream s=Assembly.GetExecutingAssembly().GetManifestResourceStream("art."+key+".png")){
                if(s==null)throw new FileNotFoundException("程序内缺少场景素材："+key);
                using(Bitmap decoded=new Bitmap(s))return new Bitmap(decoded);
            }
        }
        public static Bitmap Background(int scene){
            if(activeScene!=scene){
                Bitmap replacement=ReadEmbedded(SceneLibrary.Keys[scene]);
                if(activeBackground!=null)activeBackground.Dispose();
                activeBackground=replacement;
                activeScene=scene;
            }
            return activeBackground;
        }
        public static CharacterArt Character(int person){
            if(activePerson!=person){
                Bitmap atlas=ReadEmbedded(CharacterLibrary.Keys[person]);
                CharacterArt replacement;
                try{replacement=new CharacterArt(atlas);}
                catch{atlas.Dispose();throw;}
                if(activeCharacter!=null)activeCharacter.Atlas.Dispose();
                activeCharacter=replacement;
                activePerson=person;
            }
            return activeCharacter;
        }
    }
}
