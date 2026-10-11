using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Text;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;



namespace XzyBattleStats {
    public sealed class Role { public string Id; public string Name; public string Avatar; }
    public sealed class Player {public string Uid="",Name="";public bool IsSelf;public List<Role> Roles=new List<Role>();public long? ScoreBefore,ScoreAfter,ExportDamage,Debarrass,BeatCnt,BeatedCnt,BeatFreidnCnt,BeatAwakeCnt,AliveCnt;public double? LockedDurationRatio;}
    public sealed class SnapshotArchive {public string Format="XZY-BattleArchive";public int Version=1;public List<Snapshot> Snapshots=new List<Snapshot>();}
    public sealed class Battle {
        public string Id; public int Mode; public long Start; public string Result; public long Damage; public int Kills; public int Deaths; public bool Mvp;
        public List<Role> MyRoles=new List<Role>(); public List<Role> EnemyRoles=new List<Role>(); public List<Role> AllyRoles=new List<Role>();
        public List<Role> MyTeamRoles=new List<Role>(); public List<Role> EnemyTeamRoles=new List<Role>(); public string TeamDetailStatus="pending";
        public string AllyUid=""; public string AllyName="";public long? ScoreBefore,ScoreAfter;public List<Player> MyTeamPlayers=new List<Player>(),EnemyTeamPlayers=new List<Player>();
        [ScriptIgnore] public string ViewSourceKey="",ViewSourceLabel="",ViewGameCode="",OriginalId="";public string DetailStatus="pending"; public string DetailError=""; public string IdentityMethod="";
    }
    public sealed class Snapshot {
        public string Format="XZY-BattleStats";public int Version=1; public string Uid; public string Name; public string GameCode;
        public int SeasonCode;public string SeasonName;public long SeasonStart;public long SeasonEnd;public long DataAsOf;
        [ScriptIgnore] public List<Snapshot> ViewSources;[ScriptIgnore] public bool IsAggregate{get{return ViewSources!=null;}}public string SavedAt;public bool ListComplete;public bool Demo;public object OfficialSummary;public object ListAudit;
        public long OfficialSummaryAsOf;public string MergeNote="";public List<ImportSource> ImportSources=new List<ImportSource>();
        public List<string> Warnings=new List<string>();public List<Battle> Battles=new List<Battle>();
    }
    public sealed class ImportSource {public string Fingerprint;public long DataAsOf;public string ImportedAt;public int Records;public bool ListComplete;}
    static class J {
        public static readonly JavaScriptSerializer Json=new JavaScriptSerializer{MaxJsonLength=64*1024*1024,RecursionLimit=100};
        public static Dictionary<string,object> Obj(object x){return x as Dictionary<string,object>??new Dictionary<string,object>();}
        public static object Get(object o,string key){var d=Obj(o);object v;return d.TryGetValue(key,out v)?v:null;}
        public static string Str(object o,string key){var x=Get(o,key);return x==null?"":Convert.ToString(x,CultureInfo.InvariantCulture);}
        public static object Any(object o,params string[] keys){foreach(string k in keys){var x=Get(o,k);if(x!=null&&Convert.ToString(x)!="")return x;}return null;}
        public static string Text(object x){return x==null?"":Convert.ToString(x,CultureInfo.InvariantCulture);}
        public static long Num(object x){long v;if(!long.TryParse(Text(x),NumberStyles.Integer,CultureInfo.InvariantCulture,out v))throw new Exception("数值字段无法识别。");return v;}
        public static long Number(object o,string key,long fallback){var x=Get(o,key);return x==null?fallback:Num(x);}
        public static bool Bool(object x){if(x is bool)return(bool)x;string s=Text(x);if(s=="1"||s.Equals("true",StringComparison.OrdinalIgnoreCase))return true;if(s=="0"||s.Equals("false",StringComparison.OrdinalIgnoreCase))return false;throw new Exception("胜负字段无法识别，未将它当作失败。");}
        public static List<object> List(object x){var a=x as IEnumerable;if(a==null||x is string||x is IDictionary)throw new Exception("接口返回的列表格式不匹配。");return a.Cast<object>().ToList();}
        public static long Now(){return(DateTime.UtcNow.Ticks-new DateTime(1970,1,1).Ticks)/TimeSpan.TicksPerSecond;}
        public static string SafeName(string s){return new string(s.Where(c=>char.IsLetterOrDigit(c)||c=='-'||c=='_').Take(80).ToArray());}
        public static bool ValidMode(int n){return n==11||n==12||n==21||n==22;}
    }
    static class Parse {
        public static Battle Row(object row){
            string id=J.Str(row,"BattleId");int mode=(int)J.Number(row,"BattleType",0);long start=J.Number(row,"StartTime",0);
            if(string.IsNullOrWhiteSpace(id)||id.Length>80||!J.ValidMode(mode)||start<=0)throw new Exception("战绩 ID、模式或时间字段无法识别。");
            return new Battle{Id=id,Mode=mode,Start=start,Result=J.Bool(J.Get(row,"BattleResult"))?"win":"loss",Damage=J.Number(row,"TotalDamage",0),Kills=(int)J.Number(row,"BeatHeroNum",0),Deaths=(int)J.Number(row,"DeathHeroNum",0),Mvp=J.Get(row,"IsMvp")!=null&&J.Bool(J.Get(row,"IsMvp"))};
        }
        public static Role Character(object source,Dictionary<string,Role> catalog){
            object role=J.Get(source,"Role")??source;
            string id=J.Text(J.Any(role,"Id","id","RoleId","role_id","RoleID","HeroId","hero_id","roleId"));
            if(id=="")id=J.Text(J.Any(source,"RoleId","RoleID","HeroId","hero_id","role_id","roleId"));
            string name=J.Text(J.Any(role,"Name","name","RoleName","role_name","Title","title","roleName"));
            if(name=="")name=J.Text(J.Any(source,"RoleName","role_name","roleName"));
            if(id==""&&name!=""){var known=catalog.Values.FirstOrDefault(r=>r.Name==name);if(known!=null)id=known.Id;else id="name:"+name;}
            Role mapped;if(id!=""&&catalog.TryGetValue(id,out mapped)&&name=="")name=mapped.Name;
            if(id=="")throw new Exception("角色字段无法识别，本场不纳入角色对位表。");
            if(name=="")name="角色 #"+id;
            var result=new Role{Id=id,Name=name,Avatar=J.Text(J.Any(role,"avatar_link_xcx","avatar_link","Avatar"))};catalog[id]=result;return result;
        }
        public static List<Role> Roster(object list,Dictionary<string,Role> catalog){return J.List(list).Select(x=>Character(x,catalog)).GroupBy(r=>r.Id).Select(g=>g.First()).ToList();}
        static bool SameIdentity(object member,string uid){
            var user=J.Get(member,"UserInfo");
            return new[]{"Id","Uid","UID","UserId","UserID","RoleId","RoleID","role_id","uid","user_id","UserUid","RoleUid","roleId"}.Any(k=>uid!=""&&(J.Str(user,k)==uid||J.Str(member,k)==uid));
        }
        static string MemberUid(object member){
            var keys=new[]{"Uid","UID","RoleUid","UserUid","RoleId","RoleID","role_id","roleId","UserId","UserID","user_id","uid","Id"};
            foreach(var source in new[]{J.Get(member,"UserInfo"),member})foreach(string key in keys){string value=J.Str(source,key);if(!string.IsNullOrWhiteSpace(value)&&value!="0")return value;}return "";
        }
        public static long? Score(object value){long n;return value!=null&&long.TryParse(J.Text(value),out n)&&n>=0&&n<=1000000?(long?)n:null;}
        public static readonly string[] MetricFields=new[]{"ExportDamage","Debarrass","BeatCnt","BeatedCnt","BeatFreidnCnt","BeatAwakeCnt","AliveCnt"};
        public static long? Metric(object value){long n;return value!=null&&long.TryParse(J.Text(value),out n)&&n>=0&&n<=9007199254740991L?(long?)n:null;}
        public static double? LockRatio(object value){double n;return value!=null&&double.TryParse(J.Text(value),NumberStyles.Float,CultureInfo.InvariantCulture,out n)&&!double.IsNaN(n)&&!double.IsInfinity(n)&&n>=0&&n<=10000?(double?)n:null;}
        public static int MetricCount(Player p){return MetricFields.Count(k=>typeof(Player).GetField(k).GetValue(p)!=null)+(p.LockedDurationRatio.HasValue?1:0);}
        static Player PlayerInfo(object member,List<Role> roles,int mode,bool self){object user=J.Get(member,"UserInfo"),info=J.Get(member,mode<20?"ExportInfo":"BattleInfo");return new Player{ExportDamage=Metric(J.Get(info,mode<20?"DamageCnt":"ExportDamage")),Debarrass=Metric(J.Get(info,"Debarrass")),BeatCnt=Metric(J.Get(info,"BeatCnt")),BeatedCnt=Metric(J.Get(info,"BeatedCnt")),BeatFreidnCnt=Metric(J.Get(info,"BeatFreidnCnt")),BeatAwakeCnt=Metric(J.Get(info,mode<20?"BeatAwakeAneryCnt":"BeatAwakeCnt")),AliveCnt=Metric(J.Get(info,"AliveCnt")),LockedDurationRatio=LockRatio(J.Get(user,"LockedDurationRatio")),Uid=MemberUid(member),Name=J.Str(user,"Name"),Roles=roles,IsSelf=self,ScoreBefore=mode==12||mode==22?Score(J.Get(user,"ScoreBefore")):null,ScoreAfter=mode==12||mode==22?Score(J.Get(user,"ScoreAfter")):null};}
        public static void Detail(Battle battle,object detail,string uid,string name,Dictionary<string,Role> catalog){
            if(J.Get(detail,"BattleType")!=null && (int)J.Num(J.Get(detail,"BattleType"))!=battle.Mode)throw new Exception("详情模式与战绩列表不同。");
            bool win=battle.Result=="win";var own=new List<Role>();var enemy=new List<Role>();var ally=new List<Role>();string method="";
            if(battle.Mode==11||battle.Mode==12){
                var mine=J.Get(detail,win?"WinnerDetail":"LoserDetail");var other=J.Get(detail,win?"LoserDetail":"WinnerDetail");
                own=Roster(J.Get(mine,"RoleDamageList"),catalog);enemy=Roster(J.Get(other,"RoleDamageList"),catalog);method="lineup";
                if(own.Count<1||own.Count>3||enemy.Count<1||enemy.Count>3)throw new Exception("1v1 阵容人数异常。");battle.MyTeamPlayers=new List<Player>{PlayerInfo(mine,own,battle.Mode,true)};battle.EnemyTeamPlayers=new List<Player>{PlayerInfo(other,enemy,battle.Mode,false)};battle.ScoreBefore=battle.MyTeamPlayers[0].ScoreBefore;battle.ScoreAfter=battle.MyTeamPlayers[0].ScoreAfter;
            }else{
                var team=J.List(J.Get(detail,win?"WinnerDetails":"LoserDetails"));var opponents=J.List(J.Get(detail,win?"LoserDetails":"WinnerDetails"));
                if(team.Count!=2||opponents.Count!=2)throw new Exception("2v2 队伍人数异常。");
                // Keep both members, including duplicate characters, for team combinations.
                var teamRoles=team.Select(x=>Character(J.Get(x,"BattleInfo"),catalog)).ToList();
                var opponentRoles=opponents.Select(x=>Character(J.Get(x,"BattleInfo"),catalog)).ToList();
                battle.MyTeamRoles=teamRoles;battle.EnemyTeamRoles=opponentRoles;battle.TeamDetailStatus="ok";battle.MyTeamPlayers=team.Select((x,i)=>PlayerInfo(x,new List<Role>{teamRoles[i]},battle.Mode,false)).ToList();battle.EnemyTeamPlayers=opponents.Select((x,i)=>PlayerInfo(x,new List<Role>{opponentRoles[i]},battle.Mode,false)).ToList();
                var matches=team.Where(x=>SameIdentity(x,uid)).ToList();method="uid";
                if(matches.Count==0){matches=team.Where(x=>name!=""&&J.Str(J.Get(x,"UserInfo"),"Name")==name).ToList();method="name";}
                if(matches.Count==0){matches=team.Where(x=>{var info=J.Get(x,"BattleInfo");return J.Get(info,"ExportDamage")!=null&&J.Get(info,"BeatCnt")!=null&&J.Get(info,"BeatedCnt")!=null&&J.Num(J.Get(info,"ExportDamage"))==battle.Damage&&J.Num(J.Get(info,"BeatCnt"))==battle.Kills&&J.Num(J.Get(info,"BeatedCnt"))==battle.Deaths;}).ToList();method="personal-metrics";}
                if(matches.Count!=1)throw new Exception("无法唯一识别本场的你；未把队友角色算作你的角色。");
                var self=battle.MyTeamPlayers[team.IndexOf(matches[0])];self.IsSelf=true;battle.ScoreBefore=self.ScoreBefore;battle.ScoreAfter=self.ScoreAfter;own.Add(Character(J.Get(matches[0],"BattleInfo"),catalog));
                foreach(var member in team)if(!object.ReferenceEquals(member,matches[0])){ally.Add(Character(J.Get(member,"BattleInfo"),catalog));battle.AllyUid=MemberUid(member);battle.AllyName=J.Str(J.Get(member,"UserInfo"),"Name");}
                enemy=opponentRoles.GroupBy(r=>r.Id).Select(g=>g.First()).ToList();
            }
            battle.MyRoles=own;battle.EnemyRoles=enemy;battle.AllyRoles=ally;battle.IdentityMethod=method;battle.DetailStatus="ok";battle.DetailError="";
        }
        public static void Catalog(object data,Dictionary<string,Role> catalog,int depth){
            if(depth>8||data==null)return;var d=data as Dictionary<string,object>;
            if(d!=null){string id=J.Text(J.Any(d,"Id","id","RoleId","role_id"));string name=J.Text(J.Any(d,"Name","name","role_name","RoleName"));long n;if(long.TryParse(id,out n)&&n>0&&n<10000&&name!="")catalog[id]=new Role{Id=id,Name=name};foreach(var value in d.Values)Catalog(value,catalog,depth+1);}
            else if(!(data is string)&&data is IEnumerable)foreach(var value in(IEnumerable)data)Catalog(value,catalog,depth+1);
        }
    }
    static class Storage {
        public static Snapshot Load(string path){if(new FileInfo(path).Length>64*1024*1024)throw new Exception("数据文件过大。");var s=J.Json.Deserialize<Snapshot>(File.ReadAllText(path));Upgrade(s);Validate(s);return s;}
        public static void Upgrade(Snapshot s){
            if(s==null||s.Battles==null)return;
            foreach(var b in s.Battles){
                if(b==null||b.Mode<20||b.DetailStatus!="ok"||b.MyRoles==null||b.AllyRoles==null||b.EnemyRoles==null)continue;
                // v1.1 only deduplicated enemies after verifying exactly two opponents.
                if(b.MyTeamRoles!=null&&b.EnemyTeamRoles!=null&&b.MyTeamRoles.Count==0&&b.EnemyTeamRoles.Count==0&&b.MyRoles.Count==1&&b.AllyRoles.Count==1&&(b.EnemyRoles.Count==1||b.EnemyRoles.Count==2)){
                    b.MyTeamRoles=b.MyRoles.Concat(b.AllyRoles).ToList();b.EnemyTeamRoles=b.EnemyRoles.Count==1?new List<Role>{b.EnemyRoles[0],b.EnemyRoles[0]}:b.EnemyRoles.ToList();b.TeamDetailStatus="ok";
                }
            }
        }
        public static void Validate(Snapshot s){
            if(s==null||s.Format!="XZY-BattleStats"||s.Version!=1||s.Battles==null||s.SeasonStart<=0||s.SeasonEnd<=s.SeasonStart||s.Battles.Count>150000)throw new Exception("数据文件格式不匹配。");
            var ids=new HashSet<string>();foreach(var b in s.Battles){if(b==null||string.IsNullOrWhiteSpace(b.Id)||!ids.Add(b.Id)||!J.ValidMode(b.Mode)||b.Start<s.SeasonStart||b.Start>s.SeasonEnd||(b.Result!="win"&&b.Result!="loss")||b.MyRoles==null||b.EnemyRoles==null||b.AllyRoles==null||b.MyTeamRoles==null||b.EnemyTeamRoles==null)throw new Exception("数据含有重复或无效战绩。");if(b.DetailStatus=="ok"&&(b.MyRoles.Count==0||b.EnemyRoles.Count==0||b.MyRoles.Concat(b.EnemyRoles).Concat(b.AllyRoles).Any(r=>r==null||string.IsNullOrWhiteSpace(r.Id)||r.Name==null)))throw new Exception("角色数据缺失。");if(b.TeamDetailStatus=="ok"&&(b.Mode<20||b.MyTeamRoles.Count!=2||b.EnemyTeamRoles.Count!=2||b.MyTeamRoles.Concat(b.EnemyTeamRoles).Any(r=>r==null||string.IsNullOrWhiteSpace(r.Id)||r.Name==null)))throw new Exception("队伍组合数据缺失。");}
            foreach(var b in s.Battles){if(b.MyTeamPlayers==null)b.MyTeamPlayers=new List<Player>();if(b.EnemyTeamPlayers==null)b.EnemyTeamPlayers=new List<Player>();if((b.ScoreBefore.HasValue||b.ScoreAfter.HasValue)&&(b.Mode!=12&&b.Mode!=22))throw new Exception("匹配分数字段无效");foreach(var value in new[]{b.ScoreBefore,b.ScoreAfter})if(value.HasValue&&(value<0||value>1000000))throw new Exception("分数字段无效");foreach(var team in new[]{b.MyTeamPlayers,b.EnemyTeamPlayers}){int size=b.Mode<20?1:2;if(team.Count!=0&&team.Count!=size)throw new Exception("玩家人数无效");foreach(var p in team){if(p==null||p.Roles==null||p.Roles.Count==0||p.Roles.Count>(b.Mode<20?3:1)||p.Roles.Any(r=>r==null||string.IsNullOrWhiteSpace(r.Id)||r.Name==null))throw new Exception("玩家角色无效");foreach(var value in new[]{p.ScoreBefore,p.ScoreAfter})if(value.HasValue&&(value<0||value>1000000||(b.Mode!=12&&b.Mode!=22)))throw new Exception("玩家分数无效");foreach(string key in Parse.MetricFields){object value=typeof(Player).GetField(key).GetValue(p);if(value!=null&&!Parse.Metric(value).HasValue)throw new Exception("玩家详细统计字段无效");}if(p.LockedDurationRatio.HasValue&&!Parse.LockRatio(p.LockedDurationRatio).HasValue)throw new Exception("玩家吸锁定字段无效");}}if(b.EnemyTeamPlayers.Any(p=>p.IsSelf)||b.MyTeamPlayers.Count(p=>p.IsSelf)>1)throw new Exception("本人标记无效");}
            if(s.Warnings==null)s.Warnings=new List<string>();
            if(s.ImportSources==null)s.ImportSources=new List<ImportSource>();
        }
        public static void Save(string path,Snapshot s){Validate(s);s.SavedAt=DateTime.UtcNow.ToString("o");string temp=path+".tmp-"+Guid.NewGuid().ToString("N");Directory.CreateDirectory(Path.GetDirectoryName(path));try{File.WriteAllText(temp,J.Json.Serialize(s),new UTF8Encoding(false));Load(temp);if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);}finally{if(File.Exists(temp))File.Delete(temp);}}
    }

}
