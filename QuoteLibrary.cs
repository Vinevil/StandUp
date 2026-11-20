using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace StandUp {
    public sealed class Saying {
        public readonly string Id,Person,Text,Source,Url;
        public Saying(string id,string person,string text,string source,string url){Id=id;Person=person;Text=text;Source=source;Url=url;}
    }
    public static class Sayings {
        public static readonly Saying[] All=Build();
        static Saying[] Build(){
            List<Saying> all=new List<Saying>{
                new Saying("classic-mao-1","毛泽东","星星之火，可以燎原。","《星星之火，可以燎原》·1930","https://www.marxists.org/chinese/maozedong/marxist.org-chinese-mao-19300105.htm"),
                new Saying("classic-mao-2","毛泽东","没有调查，没有发言权。","《反对本本主义》·1930（小标题）","https://www.marxists.org/chinese/maozedong/marxist.org-chinese-mao-193005.htm"),
                new Saying("classic-mao-3","毛泽东","世上无难事，只要肯登攀。","《水调歌头·重上井冈山》·1965","https://www.12371.cn/2021/05/21/ARTI1621585726403459.shtml"),
                new Saying("classic-zhou-1","周恩来","健全自己身体，保持合理的规律生活","《我的修养要则》·1943（摘句）","https://www.marxists.org/chinese/zhouenlai/021.htm"),
                new Saying("classic-zhou-2","周恩来","人生有限，知识无限，到死也学不完，改造不完。","《论知识分子问题》·1962","https://www.marxists.org/chinese/zhouenlai/136.htm"),
                new Saying("classic-zhou-3","周恩来","活到老，学到老，改造到老。","共产党员网·2013（转引）","https://news.12371.cn/2013/04/28/ARTI1367158965258326.shtml")
            };
            Add(all,"action","行动",new[]{
                "先做一个小动作，让今天从这里开始。",
                "不必等状态完美，先完成眼前的一小步。",
                "把很大的目标，拆成今天做得到的事情。",
                "动手之后，模糊的想法才会变得清楚。",
                "今天的一点行动，比明天的许多打算更具体。",
                "给计划一个起点，也给自己一个机会。",
                "先把第一步走稳，再想远处的路。",
                "有些答案，要在尝试中慢慢找到。",
                "做完一件小事，也值得认真肯定。",
                "把注意力放在能改变的那一件事上。",
                "方向可以边走边调整，脚步可以现在开始。",
                "不需要一下做很多，只需要开始做一点。",
                "从最容易的一步开始，让行动接上行动。",
                "一个小小的完成，能让下一步更有底气。",
                "今天能推进一点，就把这一点做好。"
            });
            Add(all,"persist","坚持",new[]{
                "走得慢一些，也可以走得很远。",
                "坚持不必声势浩大，按自己的节奏继续就好。",
                "偶尔停下来，不会抹去你走过的路。",
                "今天没有白费，你又积累了一点经验。",
                "允许进度有起伏，仍然可以保持方向。",
                "一小步接着一小步，路就渐渐长起来。",
                "不是每一天都耀眼，但每一天都能有一点积累。",
                "困难的时候，把目标缩小，把脚步留下。",
                "认真做过的事情，会成为下一次的底气。",
                "短暂的停顿之后，依然可以重新出发。",
                "把今天过扎实，就是在为明天铺路。",
                "重复练习的时候，变化也在悄悄发生。",
                "给努力一点时间，不急着要求回声。",
                "坚持可以温和，不必总靠咬牙。",
                "回头看看，你已经走过了不少路。"
            });
            Add(all,"learn","学习",new[]{
                "今天弄懂一个问题，也是一种进步。",
                "暂时不懂的地方，是下一次学习的入口。",
                "把疑问写下来，让思考有一个落点。",
                "多问一个为什么，理解就可能再深一点。",
                "把学到的东西用一次，比只记住它更踏实。",
                "错误可以留下线索，帮你找到更好的方法。",
                "先弄清问题，再寻找漂亮的答案。",
                "读得慢一点，也可以想得深一点。",
                "用自己的话讲一遍，看看哪里还不清楚。",
                "你不必立刻精通，愿意学习就有下一步。",
                "把好奇心留下，路上就多了一盏灯。",
                "看见自己的盲点，也是学习的收获。",
                "给新知识一点消化的时间。",
                "方法可以更新，已经付出的探索不会消失。",
                "记录一个发现，让今天的学习留下痕迹。"
            });
            Add(all,"focus","专注",new[]{
                "先照顾眼前这一件事，再迎接下一件。",
                "把纷乱的念头放到纸上，让眼前清楚一点。",
                "给重要的事情，留一小段不被打扰的时间。",
                "做得专心，不一定要做得很久。",
                "一次处理一个问题，也是一种有效的节奏。",
                "先分清轻重，再安排力气。",
                "少做一点无关的事，给想做的事多一点空间。",
                "清楚下一步，往往比想清全部更有帮助。",
                "把今天最重要的一件事写在最前面。",
                "专注可以从收起一个干扰开始。",
                "让目标具体一点，让行动轻一点。",
                "不必同时走很多条路，先走好脚下这一条。",
                "整理一下手边，也整理一下思绪。",
                "有时减少任务，反而能把重要的事做扎实。",
                "认真对待一小段时间，也能留下好成果。"
            });
            Add(all,"rest","休息",new[]{
                "休息也是今天计划里值得保留的一部分。",
                "给自己一点空隙，事情可以稍后继续。",
                "认真工作，也认真照顾自己的感受。",
                "不必把每一分钟都填满。",
                "你可以停一会儿，再决定下一步。",
                "完成任务之前，也别忘了照顾自己。",
                "把节奏放缓一点，让今天更从容。",
                "离开屏幕片刻，看看窗外的光。",
                "留一点余地给自己，不必一直赶路。",
                "一段短暂的休息，也可以被安心地享受。",
                "进度很重要，你的感受也很重要。",
                "允许自己调整节奏，而不是责备自己。",
                "今天的安排里，也留一个自己的位置。",
                "不需要证明忙碌，休息时就安心休息。",
                "暂时放下手边的事，不等于放弃目标。"
            });
            Add(all,"courage","勇气",new[]{
                "紧张也没关系，你仍然可以尝试。",
                "第一次不够熟练，是很自然的开始。",
                "勇气有时只是愿意再试一次。",
                "未知的路，可以先走一小段看看。",
                "你不必没有顾虑，才允许自己出发。",
                "把失败当作信息，再决定怎样调整。",
                "需要帮助的时候，开口也是一种勇气。",
                "允许自己做一个还在练习的人。",
                "先尝试，再用结果修正想法。",
                "有些门，试着敲一敲才知道会不会打开。",
                "表达一个想法，也是在给可能性一个机会。",
                "没做好的一次，不会定义全部的你。",
                "即使步子很小，跨出去也需要勇气。",
                "给陌生的事情一次接近的机会。",
                "今天敢于面对一点，就值得肯定一点。"
            });
            Add(all,"patience","耐心",new[]{
                "有些成长，需要慢慢看见。",
                "先把根扎稳，再期待枝叶。",
                "不急着和别人比较，先看看自己的变化。",
                "看不到结果的时候，也可以检查方法和方向。",
                "给复杂的事情，多一点理解的时间。",
                "把期待放长一点，把今天过实一点。",
                "不用今天就回答未来所有的问题。",
                "让事情一点点变好，也是一种好。",
                "积累常常很安静，但它并不是没有发生。",
                "等一等结论，再多看一点证据。",
                "慢慢来，也要认真来。",
                "与其催促自己，不如把下一步安排清楚。",
                "进步不总是直线，曲折也可以通向前方。",
                "用适合自己的速度，完成属于自己的路。",
                "今天还没做到的，可以留给下一次继续练习。"
            });
            Add(all,"kindness","自我鼓励",new[]{
                "你已经在认真生活，这件事值得被看见。",
                "不完美的今天，也可以有值得珍惜的地方。",
                "给自己一句肯定，不必等别人先开口。",
                "看到不足，也别漏掉已经做到的部分。",
                "你可以对目标认真，也对自己温柔。",
                "不用把一次失误，变成对自己的全部评价。",
                "先接纳此刻的状态，再做一点调整。",
                "你走过的困难，不会因为没有掌声就失去分量。",
                "允许自己有普通的一天。",
                "把目光从差距移开一会儿，看看自己的收获。",
                "今天值得感谢的，也包括坚持到这里的自己。",
                "为一件小事感到开心，不需要额外的理由。",
                "别人的速度，不必成为你的刻度。",
                "把责备换成一个具体的改进，会更有力量。",
                "希望你今天既有一点收获，也有一点轻松。"
            });
            return all.ToArray();
        }
        static void Add(List<Saying> all,string key,string topic,string[] lines){for(int i=0;i<lines.Length;i++)all.Add(new Saying("original-"+key+"-"+(i+1).ToString("00"),"每日鼓励",lines[i],"原创 · "+topic,""));}
    }
    public sealed class QuotePicker {
        public static readonly string StatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "quotes-state.xml");
        public static readonly string SourcePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "quotes.txt");
        private const int MaxSourceBytes = 1024 * 1024;
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);
        private static readonly string DefaultSignature = Signature(Sayings.All);
        private static readonly Saying EmptyFallback = new Saying(
            "empty-reminder", "每日鼓励", "给自己留一点休息时间，起来活动一下。", "", "");

        private readonly Random random;
        private readonly string sourcePath;
        private readonly List<Saying> pending = new List<Saying>();
        private Saying[] pool = Sayings.All;
        private string signature = DefaultSignature;
        private string lastId = "";
        private DateTime sourceWriteTime;
        private long sourceLength = long.MinValue;

        public int Remaining { get { return pending.Count; } }

        public QuotePicker() : this(new Random(), SourcePath) { }
        public QuotePicker(Random random) : this(random, null) { }
        public QuotePicker(Random random, string sourcePath) {
            this.random = random;
            this.sourcePath = sourcePath;
            ReloadLibrary();
        }

        public static bool EnsureSourceFile() {
            if (File.Exists(SourcePath)) return true;
            try {
                using (FileStream file = new FileStream(SourcePath, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
                using (StreamWriter writer = new StreamWriter(file, new UTF8Encoding(true))) {
                    foreach (Saying saying in Sayings.All) writer.WriteLine(saying.Text);
                }
                return true;
            } catch (IOException) {
                return File.Exists(SourcePath);
            } catch (UnauthorizedAccessException) {
                return false;
            }
        }

        public bool ReloadLibrary() { return RefreshLibrary(true); }

        private bool RefreshLibrary(bool force) {
            if (sourcePath == null) return false;
            try {
                FileInfo source = new FileInfo(sourcePath);
                long length = source.Exists ? source.Length : -1;
                DateTime writeTime = source.Exists ? source.LastWriteTimeUtc : DateTime.MinValue;
                if (!force && sourceLength == length && sourceWriteTime == writeTime) return false;
                Saying[] updated = length >= 0 && length <= MaxSourceBytes ? ReadSource() : Sayings.All;
                sourceLength = length;
                sourceWriteTime = writeTime;
                return ApplyLibrary(updated);
            } catch (IOException) {
                return ApplyLibrary(Sayings.All);
            } catch (UnauthorizedAccessException) {
                return ApplyLibrary(Sayings.All);
            } catch (DecoderFallbackException) {
                return ApplyLibrary(Sayings.All);
            }
        }

        private Saying[] ReadSource() {
            Dictionary<string, Saying> builtIn = new Dictionary<string, Saying>(StringComparer.Ordinal);
            foreach (Saying saying in Sayings.All) builtIn.Add(saying.Text, saying);
            List<Saying> entries = new List<Saying>();
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            string contents = File.ReadAllText(sourcePath, Utf8);
            using (StringReader reader = new StringReader(contents))
            using (SHA256 hash = SHA256.Create()) {
                string line;
                while ((line = reader.ReadLine()) != null) {
                    string value = line.Trim();
                    if (value.Length == 0 || !seen.Add(value)) continue;
                    Saying existing;
                    if (builtIn.TryGetValue(value, out existing)) entries.Add(existing);
                    else entries.Add(new Saying("user-" + Digest(hash, value), "自定义语录", value, "", ""));
                }
            }
            return entries.Count == 0 ? new[] { EmptyFallback } : entries.ToArray();
        }

        private bool ApplyLibrary(Saying[] updated) {
            string updatedSignature = object.ReferenceEquals(updated, Sayings.All) ? DefaultSignature : Signature(updated);
            if (string.Equals(signature, updatedSignature, StringComparison.Ordinal)) return false;
            pool = updated;
            signature = updatedSignature;
            pending.Clear();
            return true;
        }

        private static string Signature(Saying[] sayings) {
            string[] ids = Array.ConvertAll(sayings, saying => saying.Id);
            Array.Sort(ids, StringComparer.Ordinal);
            using (SHA256 hash = SHA256.Create()) return Digest(hash, string.Join("\n", ids));
        }

        private static string Digest(SHA256 algorithm, string value) {
            return Convert.ToBase64String(algorithm.ComputeHash(Encoding.UTF8.GetBytes(value)));
        }

        public Saying Pick() {
            RefreshLibrary(false);
            if (pending.Count == 0) {
                pending.AddRange(pool);
                for (int index = pending.Count - 1; index > 0; index--) {
                    Swap(index, random.Next(index + 1));
                }
                int last = pending.Count - 1;
                if (last > 0 && pending[last].Id == lastId) Swap(last, random.Next(last));
            }
            int next = pending.Count - 1;
            Saying chosen = pending[next];
            pending.RemoveAt(next);
            lastId = chosen.Id;
            return chosen;
        }

        private void Swap(int first, int second) {
            Saying temporary = pending[first];
            pending[first] = pending[second];
            pending[second] = temporary;
        }

        public bool Save(string path) {
            string temporary = path + ".tmp";
            try {
                XElement root = new XElement("Quotes",
                    new XAttribute("Version", 2), new XAttribute("Pool", signature), new XAttribute("Last", lastId));
                foreach (Saying saying in pending) root.Add(new XElement("Next", saying.Id));
                new XDocument(root).Save(temporary);
                if (File.Exists(path)) File.Replace(temporary, path, null);
                else File.Move(temporary, path);
                return true;
            } catch (IOException) { } catch (UnauthorizedAccessException) { }
            try { File.Delete(temporary); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            return false;
        }

        public bool Load(string path) {
            try {
                if (!File.Exists(path)) return false;
                XElement root;
                using (XmlReader reader = XmlReader.Create(path, new XmlReaderSettings {
                    DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 16 * 1024 * 1024
                })) root = XDocument.Load(reader).Root;
                if (root == null || root.Name != "Quotes") return false;
                string version = (string)root.Attribute("Version");
                bool matches = version == "2" && (string)root.Attribute("Pool") == signature;
                bool legacy = version == "1" && signature == DefaultSignature && (string)root.Attribute("PoolSize") == pool.Length.ToString();
                if (!matches && !legacy) return false;

                Dictionary<string, Saying> known = new Dictionary<string, Saying>(StringComparer.Ordinal);
                foreach (Saying saying in pool) known.Add(saying.Id, saying);
                string previous = (string)root.Attribute("Last") ?? "";
                List<Saying> restored = new List<Saying>();
                HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
                foreach (XElement item in root.Elements("Next")) {
                    Saying saying;
                    string id = item.Value;
                    if (!known.TryGetValue(id, out saying) || !seen.Add(id) || id == previous) return false;
                    restored.Add(saying);
                }
                pending.Clear();
                pending.AddRange(restored);
                lastId = known.ContainsKey(previous) ? previous : "";
                return true;
            } catch (IOException) { } catch (UnauthorizedAccessException) { } catch (XmlException) { }
            return false;
        }
    }
}
