using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
namespace XzyBattleStats {
    sealed class MergeResult {
        public Snapshot Data;public int Added,Duplicates,Improved,Retained;
        public string Description(){return Data.SeasonName+" / UID "+Data.Uid+"：新增 "+Added+" 场，重复 "+Duplicates+" 场（不重复计数），补全详情 "+Improved+" 场，保留此前独有 "+Retained+" 场；合计 "+Data.Battles.Count+" 场。";}
    }
    static class Merge {
        static T Copy<T>(T value){return J.Json.Deserialize<T>(J.Json.Serialize(value));}
        public static string Key(Snapshot s){if(string.IsNullOrWhiteSpace(s.GameCode)||string.IsNullOrWhiteSpace(s.Uid)||s.SeasonCode<=0)throw new Exception("合并需要区服、账号 UID 和赛季编号，缺失时不会猜测账号。");return J.Json.Serialize(new[]{s.GameCode,s.Uid,s.SeasonCode.ToString(),s.Demo?"demo":"real"});}
        public static string Hash(byte[] bytes){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
        public static void Source(Snapshot s,string path){string hash=Hash(File.ReadAllBytes(path));if(!s.ImportSources.Any(x=>x.Fingerprint==hash))s.ImportSources.Add(new ImportSource{Fingerprint=hash,DataAsOf=s.DataAsOf,ImportedAt=DateTime.UtcNow.ToString("o"),Records=s.Battles.Count,ListComplete=s.ListComplete});}
        static int Quality(Battle b){return (b.DetailStatus=="ok"?10000:0)+(b.TeamDetailStatus=="ok"?1:0)+(b.IdentityMethod=="uid"?1000:b.IdentityMethod=="name"?500:0)+(!string.IsNullOrWhiteSpace(b.AllyUid)?100:0)+(!string.IsNullOrWhiteSpace(b.AllyName)?10:0);}
        public static bool Improved(Battle old,Battle merged){return (old.DetailStatus!="ok"&&merged.DetailStatus=="ok")||(old.TeamDetailStatus!="ok"&&merged.TeamDetailStatus=="ok")||(string.IsNullOrWhiteSpace(old.AllyUid)&&!string.IsNullOrWhiteSpace(merged.AllyUid))||(string.IsNullOrWhiteSpace(old.AllyName)&&!string.IsNullOrWhiteSpace(merged.AllyName))||(!old.ScoreAfter.HasValue&&merged.ScoreAfter.HasValue)||(ScoreCount(merged)>ScoreCount(old));}
        static bool SameRoles(List<Role> a,List<Role> b){return a.Select(r=>r.Id).OrderBy(x=>x,StringComparer.Ordinal).SequenceEqual(b.Select(r=>r.Id).OrderBy(x=>x,StringComparer.Ordinal));}
        static int ScoreCount(Battle b){return b.MyTeamPlayers.Concat(b.EnemyTeamPlayers).Sum(p=>(p.ScoreBefore.HasValue?1:0)+(p.ScoreAfter.HasValue?1:0)+Parse.MetricCount(p));}
        static List<Player> Players(List<Player> recent,List<Player> prior){if(recent.Count==0)return Copy(prior);var result=Copy(recent);foreach(var p in result){var matches=prior.Where(q=>SameRoles(p.Roles,q.Roles)&&(!string.IsNullOrWhiteSpace(p.Uid)&&!string.IsNullOrWhiteSpace(q.Uid)?p.Uid==q.Uid:!string.IsNullOrWhiteSpace(p.Name)&&p.Name==q.Name)).ToList();if(matches.Count!=1)continue;var previous=matches[0];if(string.IsNullOrWhiteSpace(p.Uid))p.Uid=previous.Uid;if(string.IsNullOrWhiteSpace(p.Name))p.Name=previous.Name;p.IsSelf=p.IsSelf||previous.IsSelf;p.ScoreBefore=p.ScoreBefore??previous.ScoreBefore;p.ScoreAfter=p.ScoreAfter??previous.ScoreAfter;foreach(string key in Parse.MetricFields){var field=typeof(Player).GetField(key);if(field.GetValue(p)==null)field.SetValue(p,field.GetValue(previous));}p.LockedDurationRatio=p.LockedDurationRatio??previous.LockedDurationRatio;}return result;}
        static Battle Battle(Battle old,Battle incoming,bool incomingNewer){
            // A conflicting ID must never silently change winner-side or account identification.
            if(old.Mode!=incoming.Mode||old.Start!=incoming.Start||old.Result!=incoming.Result||old.Damage!=incoming.Damage||old.Kills!=incoming.Kills||old.Deaths!=incoming.Deaths||old.Mvp!=incoming.Mvp)throw new Exception("对局 ID "+old.Id+" 的模式、时间或结果字段冲突，本批次尚未保存。请保留两个源文件核对。");
            var recent=incomingNewer?incoming:old;var earlier=incomingNewer?old:incoming;
            var donor=Quality(recent)>=Quality(earlier)?recent:earlier;
            var result=Copy(donor);
            // Preserve a complete team even when only personal detail is available in the better source.
            if(result.TeamDetailStatus!="ok"&&earlier.TeamDetailStatus=="ok"){result.MyTeamRoles=Copy(earlier.MyTeamRoles);result.EnemyTeamRoles=Copy(earlier.EnemyTeamRoles);result.TeamDetailStatus="ok";}
            if(result.TeamDetailStatus!="ok"&&recent.TeamDetailStatus=="ok"){result.MyTeamRoles=Copy(recent.MyTeamRoles);result.EnemyTeamRoles=Copy(recent.EnemyTeamRoles);result.TeamDetailStatus="ok";}
            if(old.DetailStatus=="ok"&&incoming.DetailStatus=="ok"&&SameRoles(old.MyRoles,incoming.MyRoles)&&SameRoles(old.AllyRoles,incoming.AllyRoles)){
                if(!string.IsNullOrWhiteSpace(recent.AllyUid))result.AllyUid=recent.AllyUid;
                else if(!string.IsNullOrWhiteSpace(earlier.AllyUid))result.AllyUid=earlier.AllyUid;
                if(!string.IsNullOrWhiteSpace(recent.AllyName))result.AllyName=recent.AllyName;
                else if(!string.IsNullOrWhiteSpace(earlier.AllyName))result.AllyName=earlier.AllyName;
            }
            foreach(var source in new[]{recent,earlier})if(source.DetailStatus=="ok"&&SameRoles(source.MyRoles,result.MyRoles)&&source.ScoreAfter.HasValue){result.ScoreAfter=source.ScoreAfter;result.ScoreBefore=source.ScoreBefore;break;}
            result.MyTeamPlayers=Players(recent.MyTeamPlayers,earlier.MyTeamPlayers);result.EnemyTeamPlayers=Players(recent.EnemyTeamPlayers,earlier.EnemyTeamPlayers);var self=result.MyTeamPlayers.FirstOrDefault(p=>p.IsSelf);if(self!=null&&result.DetailStatus=="ok"&&SameRoles(self.Roles,result.MyRoles)){result.ScoreBefore=result.ScoreBefore??self.ScoreBefore;result.ScoreAfter=result.ScoreAfter??self.ScoreAfter;}
            return result;
        }
        public static MergeResult Combine(Snapshot old,Snapshot incoming){
            Storage.Validate(incoming);Key(incoming);
            if(old==null)return new MergeResult{Data=Copy(incoming),Added=incoming.Battles.Count};
            Storage.Validate(old);if(Key(old)!=Key(incoming))throw new Exception("不同区服、账号或赛季不能合并。");
            if(old.SeasonStart!=incoming.SeasonStart||old.SeasonEnd!=incoming.SeasonEnd)throw new Exception("同一赛季的起止时间不一致，本批次尚未保存。");
            bool newer=incoming.DataAsOf>=old.DataAsOf;var recent=newer?incoming:old;var prior=newer?old:incoming;var combined=Copy(recent);
            if(string.IsNullOrWhiteSpace(combined.Name))combined.Name=prior.Name;
            long oldSummaryAt=old.OfficialSummaryAsOf>0?old.OfficialSummaryAsOf:old.DataAsOf,incomingSummaryAt=incoming.OfficialSummaryAsOf>0?incoming.OfficialSummaryAsOf:incoming.DataAsOf;
            var summary=incoming.OfficialSummary!=null&&(old.OfficialSummary==null||incomingSummaryAt>=oldSummaryAt)?incoming:old;
            combined.OfficialSummary=summary.OfficialSummary;combined.OfficialSummaryAsOf=summary.OfficialSummary==null?0:summary==incoming?incomingSummaryAt:oldSummaryAt;
            combined.ImportSources=old.ImportSources.Concat(incoming.ImportSources).GroupBy(x=>x.Fingerprint).Select(g=>Copy(g.First())).ToList();
            var result=new MergeResult{Data=combined};var rows=old.Battles.ToDictionary(b=>b.Id,b=>Copy(b),StringComparer.Ordinal);var inputIds=new HashSet<string>(incoming.Battles.Select(b=>b.Id),StringComparer.Ordinal);
            result.Retained=old.Battles.Count(b=>!inputIds.Contains(b.Id));
            foreach(var b in incoming.Battles){Battle previous;if(!rows.TryGetValue(b.Id,out previous)){rows.Add(b.Id,Copy(b));result.Added++;}else{result.Duplicates++;var merged=Battle(previous,b,newer);if(Improved(previous,merged))result.Improved++;rows[b.Id]=merged;}}
            combined.Battles=rows.Values.OrderByDescending(b=>b.Start).ThenBy(b=>b.Id,StringComparer.Ordinal).ToList();
            Storage.Validate(combined);return result;
        }
    }
    static class Archive {
        sealed class Plan {public string Path;public MergeResult Result;}
        public static List<MergeResult> Import(string folder,IEnumerable<string> paths){
            // Decode and validate every selected file and prepare every account/season before writing.
            var inputs=paths.SelectMany(path=>Stats.DecodeMany(path).Select(s=>{Merge.Source(s,path);return s;})).ToList();
            var groups=inputs.GroupBy(Merge.Key).ToList();var existing=new List<Snapshot>();
            if(Directory.Exists(folder))foreach(string file in Directory.GetFiles(folder,"*.json")){try{existing.Add(Storage.Load(file));}catch(Exception ex){throw new Exception("本机缓存无法读取："+Path.GetFileName(file)+"。未保存本次导入。"+ex.Message);}}
            var plans=new List<Plan>();
            foreach(var group in groups){Snapshot previous=null;foreach(var s in existing.Where(s=>Merge.Key(s)==group.Key))previous=Merge.Combine(previous,s).Data;
                Snapshot batch=null;foreach(var s in group)batch=Merge.Combine(batch,s).Data;
                var result=Merge.Combine(previous,batch);result.Duplicates+=group.Sum(s=>s.Battles.Count)-batch.Battles.Count;
                var before=(previous==null?Enumerable.Empty<Battle>():previous.Battles).Concat(group.SelectMany(s=>s.Battles)).GroupBy(b=>b.Id).ToDictionary(g=>g.Key,g=>g.First());
                result.Improved=result.Data.Battles.Count(b=>Merge.Improved(before[b.Id],b));result.Data.MergeNote=result.Description();
                string fileName=J.SafeName(batch.GameCode+"-"+batch.Uid+"-S"+batch.SeasonCode)+"-"+Merge.Hash(Encoding.UTF8.GetBytes(group.Key)).Substring(0,16)+".json";
                plans.Add(new Plan{Path=Path.Combine(folder,fileName),Result=result});
            }
            foreach(var plan in plans){
                if(File.Exists(plan.Path)){string backups=Path.Combine(folder,"backups");Directory.CreateDirectory(backups);File.Copy(plan.Path,Path.Combine(backups,Path.GetFileNameWithoutExtension(plan.Path)+"-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N").Substring(0,8)+".json"));}
                Storage.Save(plan.Path,plan.Result.Data);
            }
            return plans.Select(p=>p.Result).ToList();
        }
    }
}
