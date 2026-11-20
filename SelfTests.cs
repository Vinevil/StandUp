using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace StandUp {
    public static class SelfTests {
        static int checks;
        static void Check(bool ok,string name){if(!ok)throw new Exception("Test failed: "+name);checks++;}
        [DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);

        internal static bool UsesApplicationIcons(Form form){
            const uint WM_GETICON=0x007f;
            for(int kind=0;kind<=1;kind++){
                Size size=kind==0?SystemInformation.SmallIconSize:SystemInformation.IconSize;
                IntPtr handle=SendMessage(form.Handle,WM_GETICON,(IntPtr)kind,IntPtr.Zero);
                if(handle==IntPtr.Zero)return false;
                using(Icon expected=AppMark.CreateIcon(size))
                using(Bitmap reference=expected.ToBitmap())
                using(Icon actual=Icon.FromHandle(handle))
                using(Bitmap image=actual.ToBitmap()){
                    if(image.Size!=reference.Size)return false;
                    for(int y=0;y<image.Height;y++)for(int x=0;x<image.Width;x++)
                        if(image.GetPixel(x,y)!=reference.GetPixel(x,y))return false;
                }
            }
            return true;
        }

        static void CheckCompactDock(){
            // A monitor left and above the primary, with two reserved working-area margins.
            Rectangle screen=new Rectangle(-1280,-900,1280,900);
            Rectangle work=Rectangle.FromLTRB(-1256,-868,0,0);
            Point origin=new Point(-860,-450);
            Size expanded=new Size(252,58);
            const int thickness=4, threshold=10;
            var cases=new[]{
                new {Edge=DockEdge.Left, Taskbar=AnchorStyles.Left, Full=new Rectangle(-1280,-450,252,58), Strip=new Rectangle(-1280,-450,4,58)},
                new {Edge=DockEdge.Right, Taskbar=AnchorStyles.Right, Full=new Rectangle(-252,-450,252,58), Strip=new Rectangle(-4,-450,4,58)},
                new {Edge=DockEdge.Top, Taskbar=AnchorStyles.Top, Full=new Rectangle(-860,-900,252,58), Strip=new Rectangle(-860,-900,252,4)},
                new {Edge=DockEdge.Bottom, Taskbar=AnchorStyles.Bottom, Full=new Rectangle(-860,-58,252,58), Strip=new Rectangle(-860,-4,252,4)}
            };
            foreach(var item in cases){
                Rectangle full=CompactDock.ExpandedBounds(origin,expanded,screen,work,item.Edge);
                Check(full==item.Full,"dock expanded bounds "+item.Edge);
                Check(CompactDock.StripBounds(full,item.Edge,thickness)==item.Strip,"dock strip size and location "+item.Edge);
                Check(CompactDock.NearestEdge(full,screen,AnchorStyles.None,threshold)==item.Edge,"dock on free edge "+item.Edge);
                Check(!CompactDock.IsAllowed(item.Edge,item.Taskbar) &&
                      CompactDock.NearestEdge(full,screen,item.Taskbar,threshold)==DockEdge.None,"skip taskbar edge "+item.Edge);
            }
            Rectangle corner=new Rectangle(-1278,-896,expanded.Width,expanded.Height);
            Check(CompactDock.NearestEdge(corner,screen,AnchorStyles.None,threshold)==DockEdge.Left,"nearer edge wins at a corner");
            Check(CompactDock.NearestEdge(corner,screen,AnchorStyles.Left,threshold)==DockEdge.Top,"corner uses remaining free edge");
            Check(CompactDock.NearestEdge(corner,screen,AnchorStyles.Left|AnchorStyles.Top,threshold)==DockEdge.None,"both corner edges blocked");
            Rectangle beyond=new Rectangle(screen.Left+threshold+1,origin.Y,expanded.Width,expanded.Height);
            Check(CompactDock.NearestEdge(beyond,screen,AnchorStyles.None,threshold)==DockEdge.None,"dragging beyond snap distance releases edge");
            Rectangle clamped=CompactDock.ExpandedBounds(new Point(-1400,-999),expanded,screen,work,DockEdge.Top);
            Check(clamped.Location==new Point(work.Left,screen.Top),"top dock clamps along working area");
            Check(CompactDock.StripBounds(beyond,DockEdge.None,thickness)==beyond,"undocked window keeps its full dimensions");
        }

        static void CheckCompactStartup(){
            Screen display=Screen.PrimaryScreen;
            AnchorStyles taskbars=TaskbarEdges.OnScreen(display);
            Point origin=new Point(display.WorkingArea.Left+display.WorkingArea.Width/3,display.WorkingArea.Top+display.WorkingArea.Height/3);
            foreach(DockEdge edge in CompactDock.Edges){
                if(!CompactDock.IsAllowed(edge,taskbars))continue;
                using(CompactWindow compact=new CompactWindow(2f)){
                    compact.RestorePlacement(origin,(int)edge);
                    compact.ShowCompact(origin,false);
                    Rectangle full=CompactDock.ExpandedBounds(origin,new Size(504,116),display.Bounds,display.WorkingArea,edge);
                    Check(compact.IsTucked&&compact.Bounds==CompactDock.StripBounds(full,edge,8),"first show restores thin strip on "+edge);
                    using(Region region=compact.Region.Clone()){
                        int right=compact.ClientSize.Width-1, bottom=compact.ClientSize.Height-1;
                        Check(region.IsVisible(right/2,bottom/2)&&!region.IsVisible(0,0)&&!region.IsVisible(right,0)
                            &&!region.IsVisible(0,bottom)&&!region.IsVisible(right,bottom),"strip is rounded at all four corners on "+edge);
                    }
                }
            }
        }
        public static void Run(string output){
            CheckCompactDock();
            CheckCompactStartup();
            DateTime now=new DateTime(2026,10,2,7,0,0,DateTimeKind.Utc);ReminderClock clock=new ReminderClock();int alerts=0;clock.Due+=delegate{alerts++;};
            clock.StartWork(now,TimeSpan.FromMinutes(30));Check(clock.Remaining(now)==TimeSpan.FromMinutes(30),"initial countdown");
            clock.Tick(now.AddMinutes(29));Check(clock.State==Phase.Working&&alerts==0,"before deadline");
            clock.Tick(now.AddMinutes(30));Check(clock.State==Phase.Alert&&alerts==1,"exact deadline");
            clock.Tick(now.AddHours(8));Check(alerts==1,"no repeated alerts while pending");
            clock.Snooze(now);Check(clock.State==Phase.Working&&clock.Remaining(now)==TimeSpan.FromMinutes(1),"snooze");
            clock.Tick(now.AddSeconds(60));Check(alerts==2&&clock.State==Phase.Alert,"snooze expires");
            clock.StartRest(now,TimeSpan.FromMinutes(2));clock.TogglePause(now.AddSeconds(30));Check(clock.Paused&&clock.Remaining(now.AddHours(2))==TimeSpan.FromSeconds(90),"paused rest stays frozen");
            clock.Tick(now.AddHours(2));Check(clock.State==Phase.Resting,"paused tick");
            clock.TogglePause(now.AddHours(2));Check(clock.Deadline==now.AddHours(2).AddSeconds(90),"resume from remainder");
            clock.Tick(clock.Deadline);Check(clock.State==Phase.RestDone,"rest done waits for user");clock.Tick(now.AddDays(1));Check(clock.State==Phase.RestDone,"no automatic seated restart");
            clock.StartWork(now,TimeSpan.FromMinutes(30));clock.Tick(now.AddHours(8));Check(clock.State==Phase.Alert&&alerts==3,"sleep/wake catches deadline once");
            clock.StartWork(now,TimeSpan.FromMinutes(15));clock.TogglePause(now.AddMinutes(2));clock.TogglePause(now.AddMinutes(12));Check(clock.Remaining(now.AddMinutes(12))==TimeSpan.FromMinutes(13),"work pause");
            clock.TestAlert();Check(clock.State==Phase.Alert&&!clock.Paused&&alerts==4,"test alert");
            clock.TogglePause(now);Check(!clock.Paused,"pending alert cannot be paused");
            clock.StartWork(now,TimeSpan.FromMinutes(20));Check(clock.State==Phase.Working&&!clock.Paused,"new settings reset clock");
            Check(Sayings.All.Length==126,"expanded quote count");HashSet<string> ids=new HashSet<string>(),texts=new HashSet<string>();
            foreach(Saying q in Sayings.All){Check(ids.Add(q.Id)&&texts.Add(q.Text),"unique ids and text");Check(q.Text.Length<=34,"quote fits small bubble");Check(q.Url==""?q.Person=="每日鼓励"&&q.Source.StartsWith("原创"):Uri.IsWellFormedUriString(q.Url,UriKind.Absolute),"honest authorship and source");}
            QuotePicker picker=new QuotePicker(new Random(7));string previous="";
            for(int cycle=0;cycle<3;cycle++){
                HashSet<string> shown=new HashSet<string>();for(int i=0;i<Sayings.All.Length;i++){Saying q=picker.Pick();Check(shown.Add(q.Id),"each cycle has no repeats");Check(q.Id!=previous,"cycle boundary does not repeat");previous=q.Id;}
                Check(shown.Count==Sayings.All.Length&&picker.Remaining==0,"complete pool before reshuffle");
            }
            string stateTest=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(output)),"quote-state-test.xml");
            for(int i=0;i<17;i++)picker.Pick();Check(picker.Save(stateTest),"history save");QuotePicker resumed=new QuotePicker(new Random(123));Check(resumed.Load(stateTest)&&resumed.Remaining==picker.Remaining,"history reload");
            for(int i=0;i<25;i++)Check(picker.Pick().Id==resumed.Pick().Id,"same unseen queue after restart");
            File.WriteAllText(stateTest,"<Quotes Version=\"1\" PoolSize=\"126\" Last=\"\"><Next>unknown</Next></Quotes>");Check(!resumed.Load(stateTest),"invalid history safely rejected");File.Delete(stateTest);
            Check(SceneLibrary.Names.Length==5&&SceneLibrary.Keys.Length==5,"all five scenes selectable");
            Check(CharacterLibrary.Names.Length==3&&CharacterLibrary.Keys.Length==3,"three available companions");
            Check(CharacterLibrary.FromSavedId(0)==0&&CharacterLibrary.FromSavedId(1)==0,"unsupported saved companions use the default");
            for(int p=0;p<CharacterLibrary.Names.Length;p++){
                Check(CharacterLibrary.FromSavedId(p+2)==p&&CharacterLibrary.ToSavedId(p)==p+2,"stable saved companion ID");
                CharacterArt art=ArtAssets.Character(p);
                Check(art.Atlas.GetPixel(0,0).A<=2&&art.Atlas.GetPixel(art.Atlas.Width/2,art.Atlas.Height/2).A<=2,"character transparent alpha");
                Check(art.Standing.Width>0&&art.Seated.Width>0,"both character poses");
                for(int s=0;s<5;s++)foreach(double pose in new[]{0.0,0.5,1.0})using(Bitmap b=PixelScene.RenderWorld(p,s,pose,true,3)){Check(b.Width==640&&b.Height==360,"renders all scenery and poses");}
            }
            using(MainWindow w=new MainWindow(false))Check(w.IsFirstFrameReady(),"production startup fills controls before Show");
            using(MainWindow w=new MainWindow(true))checks+=w.CheckInterface();
            File.WriteAllText(output,"PASS: "+checks+" assertions\r\nTimer: deadlines, sleep/wake, pause/resume, snooze, rest completion, explicit next cycle.\r\nQuotes: 126 unique entries, 3 full shuffle cycles without repeats, history persistence, honest attribution.\r\nArtwork: transparent embedded sprites, 3 characters x 5 scenes x 3 transition frames.\r\nInterface: 7 themes, companion selection, settings scroll and layout, next quote preserves timing, compact dock/reveal, minimized reminders.\r\n",new UTF8Encoding(false));
        }
    }
}
