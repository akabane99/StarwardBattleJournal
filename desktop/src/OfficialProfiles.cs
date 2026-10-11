using System;using System.Collections.Generic;using System.Diagnostics;using System.Drawing;using System.IO;using System.Linq;using System.Text;using System.Windows.Forms;
namespace XzyBattleStats {
static class OfficialProfiles {
 public const string Page="https://xzy.shengtiangames.com/gamerecords_2/dist/index.html#/pages/record/battle/index";
 public static string Root{get{return Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"official-web");}}
 public static string Browser(){foreach(string dir in new[]{Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)})foreach(string suffix in new[]{"Microsoft\\Edge\\Application\\msedge.exe","Google\\Chrome\\Application\\chrome.exe"}){string p=Path.Combine(dir,suffix);if(File.Exists(p))return p;}throw new Exception("未找到 Edge 或 Chrome。可使用浏览器书签指南导出后导入。");}
 public static string Slot(string root,int slot){if(slot<1||slot>8)throw new Exception("账号网页编号无效");return Path.Combine(root,"account-"+slot);}
 public static void Prepare(string root,int slot,string helper){
  string dir=Slot(root,slot),profile=Path.Combine(dir,"profile","Default"),downloads=Path.Combine(dir,"downloads");Directory.CreateDirectory(profile);Directory.CreateDirectory(downloads);
  string prefs=Path.Combine(profile,"Preferences");if(!File.Exists(prefs))File.WriteAllText(prefs,J.Json.Serialize(new{download=new{default_directory=downloads,prompt_for_download=false,directory_upgrade=true},bookmark_bar=new{show_on_all_tabs=true}}),new UTF8Encoding(false));
  string bookmarks=Path.Combine(profile,"Bookmarks");if(!File.Exists(bookmarks)){
   Func<string,string,string,object> link=(id,name,url)=>new{id=id,name=name,type="url",url=url,date_added="0",guid=Guid.NewGuid().ToString()};
   Func<string,string,object[],object> folder=(id,name,children)=>new{id=id,name=name,type="folder",children=children,date_added="0",date_modified="0",guid=Guid.NewGuid().ToString()};
   File.WriteAllText(bookmarks,J.Json.Serialize(new{version=1,roots=new{bookmark_bar=folder("1","收藏夹栏",new[]{link("4","星之翼官网 · 账号网页 "+slot,Page),link("5","导出星之翼战绩 v1.8","javascript:void(async()=>{"+helper+"})();")}),other=folder("2","其他收藏夹",new object[0]),synced=folder("3","移动收藏夹",new object[0])}}),new UTF8Encoding(false));
  }
 }
 public static void Open(int slot){RefreshBrowser.Ensure(Root,slot);string profile=Path.Combine(Slot(Root,slot),"profile");Process.Start(new ProcessStartInfo(Browser(),"--user-data-dir=\""+profile+"\" --new-window \""+Page+"\""){UseShellExecute=false});}
}
sealed partial class NativeForm {
 readonly Timer officialPoll=new Timer{Interval=5000};bool pollingOfficial;bool officialImportFailed;Label officialHint;
 Dictionary<string,string> OfficialReceipts(){try{return J.Json.Deserialize<Dictionary<string,string>>(File.ReadAllText(Path.Combine(OfficialProfiles.Root,"processed.json")))??new Dictionary<string,string>();}catch{return new Dictionary<string,string>();}}
 void StartOfficialPolling(){if(preview)return;officialPoll.Tick+=delegate{PollOfficial();};officialPoll.Start();FormClosed+=delegate{officialPoll.Stop();officialPoll.Dispose();};}
 void PollOfficial(){if(pollingOfficial||!Directory.Exists(OfficialProfiles.Root))return;pollingOfficial=true;officialImportFailed=false;try{var receipts=OfficialReceipts();for(int slot=1;slot<=8;slot++){string download=Path.Combine(OfficialProfiles.Slot(OfficialProfiles.Root,slot),"downloads");if(!Directory.Exists(download))continue;foreach(string file in Directory.GetFiles(download,"*.json").OrderBy(File.GetLastWriteTimeUtc)){
   var info=new FileInfo(file);string stamp=info.Length+":"+info.LastWriteTimeUtc.Ticks,prior;if(receipts.TryGetValue(file,out prior)&&prior==stamp)continue;
   try{if(info.Length==0||info.Length>64*1024*1024)throw new Exception("文件为空或超过 64 MB");var result=Archive.Import(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"data"),new[]{file});if(result.Count==0)throw new Exception("文件没有可导入记录");foreach(var r in result)RefreshBrowser.Remember(OfficialProfiles.Root,slot,r.Data.Uid,r.Data.GameCode,r.Data.Name);receipts[file]=stamp;File.WriteAllText(Path.Combine(OfficialProfiles.Root,"processed.json"),J.Json.Serialize(receipts),new UTF8Encoding(false));
    if(data!=null&&data.IsAggregate){var keys=new HashSet<string>(data.ViewSources.Select(Merge.Key));data=Aggregation.Create(ReadSources().Where(s=>keys.Contains(Merge.Key(s))));}else if(data==null)data=result.Last().Data;else{var same=result.FirstOrDefault(r=>Merge.Key(r.Data)==Merge.Key(data));if(same!=null)data=same.Data;}Render();status.Text="账号网页 "+slot+"：已自动导入 / 合并 "+result.Sum(r=>r.Added)+" 场新增记录。";if(officialHint!=null&&!officialHint.IsDisposed)officialHint.Text=status.Text;
   }catch(Exception ex){officialImportFailed=true;string message="账号网页 "+slot+" 待导入："+ex.Message;if(officialHint!=null&&!officialHint.IsDisposed)officialHint.Text=message;status.Text=message;}
  }}}finally{pollingOfficial=false;}}
 void OpenOfficialProfiles(){using(var dialog=new Form{Text="官网多账号网页",Width=820,Height=570,StartPosition=FormStartPosition.CenterParent,BackColor=BackColor,ForeColor=ForeColor,Font=new Font("Microsoft YaHei UI",10)}){
  var list=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoScroll=true,FlowDirection=FlowDirection.TopDown,WrapContents=false,Padding=new Padding(16)};
  list.Controls.Add(new Label{AutoSize=true,MaximumSize=new Size(750,0),Text="每个账号网页独立保存登录。首次分别登录并绑定账号；可同时打开多个窗口，各自选择赛季并导出。\n在网页收藏夹栏点击“导出星之翼战绩 v1.7”。Ctrl+Shift+B 显示收藏夹栏。\n保持分析程序打开，下载完成后会自动合并；关闭程序期间的下载，下次打开也会导入。"});
  for(int i=1;i<=8;i++){int slot=i;var row=new FlowLayoutPanel{AutoSize=true,Width=750,WrapContents=false,Margin=new Padding(0,7,0,0)};row.Controls.Add(new Label{Text="账号网页 "+i,Width=115,Padding=new Padding(0,8,0,0)});row.Controls.Add(Action("打开 / 返回网页",()=>OfficialProfiles.Open(slot)));row.Controls.Add(Action("下载文件夹",()=>{string dir=Path.Combine(OfficialProfiles.Slot(OfficialProfiles.Root,slot),"downloads");Directory.CreateDirectory(dir);Process.Start(new ProcessStartInfo(dir){UseShellExecute=true});}));list.Controls.Add(row);}
  list.Controls.Add(Action("普通浏览器书签指南",()=>OpenGuide()));officialHint=new Label{AutoSize=true,MaximumSize=new Size(750,0),Padding=new Padding(0,12,0,0),Text="每个网页只用于自己的账号；请勿在已分配的网页中切换成其他账号。"};list.Controls.Add(officialHint);dialog.Controls.Add(list);dialog.ShowDialog(this);officialHint=null;
 }}
}
}
