using System;using System.Drawing;using System.Linq;using System.Windows.Forms;
namespace XzyBattleStats {
sealed partial class NativeForm {
 Form BattleDetailForm(Battle b){
  // Shared user theme.
  Color ink=dark?Color.FromArgb(224,235,244):Color.FromArgb(30,50,65),muted=dark?Color.LightSteelBlue:Color.SlateGray;
  float scale=(float)Math.Max(.65,uiScale);
  var dialog=new Form{Text="对局详情",Size=new Size(780,900),MinimumSize=new Size(680,600),StartPosition=FormStartPosition.CenterParent,BackColor=BackColor,ForeColor=ink,Font=new Font("Microsoft YaHei UI",Math.Max(6,10*scale)),AutoScaleMode=AutoScaleMode.None};
  var title=new Label{Dock=DockStyle.Top,Height=70,Padding=new Padding(15),Text=Stats.Label(b.Mode)+" · "+Beijing(b.Start)+" · "+(b.Result=="win"?"胜利":"失败")+" · 本人 "+(b.Mode!=12&&b.Mode!=22?"匹配 · 无积分":b.ScoreBefore.HasValue&&b.ScoreAfter.HasValue?(b.ScoreAfter-b.ScoreBefore>0?"+":"")+(b.ScoreAfter-b.ScoreBefore)+" 分":"增减未记录")+"\n对局 ID "+(b.OriginalId==""?b.Id:b.OriginalId)};
  var detailCards=new System.Collections.Generic.List<TableLayoutPanel>();var scroll=new Panel{Dock=DockStyle.Fill,AutoScroll=true};
  var teams=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,ColumnCount=2,Padding=new Padding(12)};teams.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));teams.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));
  for(int side=0;side<2;side++){
   var group=new FlowLayoutPanel{Dock=DockStyle.Top,AutoSize=true,FlowDirection=FlowDirection.TopDown,WrapContents=false,Padding=new Padding(12),BackColor=side==0?(dark?Color.FromArgb(27,52,70):Color.FromArgb(240,246,250)):(dark?Color.FromArgb(60,43,50):Color.FromArgb(252,243,240))};
   group.Controls.Add(new Label{AutoSize=true,Text=side==0?"我方队伍":"敌方队伍",Font=new Font(dialog.Font,FontStyle.Bold)});
   var ps=RecordCanvas.Team(b,side==0);for(int at=0;at<ps.Count;at++){
    Player p=ps[at];string label=side==0?(p.IsSelf?"本人":"队友"):"对手 "+(at+1);
    var card=new TableLayoutPanel{AutoSize=true,ColumnCount=2,Width=320,Padding=new Padding(4),Margin=new Padding(0,12,0,12)};card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));
    var head=new FlowLayoutPanel{AutoSize=true,FlowDirection=FlowDirection.LeftToRight,WrapContents=true,Width=300};if(side==1&&data!=null){var opponent=OpponentStats.Rows(data,mode).FirstOrDefault(o=>o.Identity==OpponentStats.Key(b,p,data.GameCode));if(opponent!=null)head.Controls.Add(new Label{AutoSize=true,Text="交手 "+opponent.Games+" 场 · 你的胜率 "+(100.0*opponent.Wins/opponent.Games).ToString("0.0")+"%",Padding=new Padding(0,5,8,5)});}head.Controls.Add(new Label{AutoSize=true,Text=label+" · "+p.Name,Padding=new Padding(0,5,8,5)});foreach(var r in p.Roles){head.Controls.Add(new PictureBox{Image=AvatarImages.Get(r),Size=new Size((int)Math.Round(35*scale*1.5),(int)Math.Round(35*scale*1.5)),SizeMode=PictureBoxSizeMode.Zoom});head.Controls.Add(new Label{AutoSize=true,Text=r.Name,Padding=new Padding(0,5,8,5)});}card.Controls.Add(head,0,0);card.SetColumnSpan(head,2);
    string[] keys={"ExportDamage","Debarrass","BeatCnt","BeatedCnt","BeatFreidnCnt","BeatAwakeCnt","LockedDurationRatio","AliveCnt"};string[] names={"输出","解围","击败","被击败","击败友军","击败觉醒敌人","吸锁定比例","存活角色"};
    for(int i=0;i<(b.Mode<20?8:7);i++){object value=typeof(Player).GetField(keys[i]).GetValue(p);string text=value==null?"未记录":keys[i]=="LockedDurationRatio"?((double)value*100).ToString("0.00")+"%":Convert.ToString(value);card.Controls.Add(new Label{Text=names[i]+"："+text,AutoSize=true,Padding=new Padding(0,4,10,4)},i%2,1+i/2);}
    if(b.Mode==12||b.Mode==22){var score=new Label{AutoSize=true,Padding=new Padding(0,7,0,0),Text="分数："+RecordCanvas.Score(p.ScoreBefore)+" → "+RecordCanvas.Score(p.ScoreAfter)};card.Controls.Add(score,0,5);card.SetColumnSpan(score,2);}detailCards.Add(card);group.Controls.Add(card);
   }teams.Controls.Add(group,side,0);
  }scroll.Controls.Add(teams);dialog.Controls.Add(scroll);dialog.Controls.Add(title);Action fit=()=>{int width=Math.Max(220,(scroll.ClientSize.Width-100)/2);foreach(var card in detailCards){card.Width=width;var head=card.Controls.OfType<FlowLayoutPanel>().First();head.Width=width-8;foreach(var label in card.Controls.OfType<Label>())label.MaximumSize=new Size(width/2-10,0);}teams.PerformLayout();};dialog.Shown+=delegate{fit();};scroll.Resize+=delegate{fit();};dialog.Controls.Add(Fold("详情说明",new Label{Dock=DockStyle.Bottom,Height=45,Padding=new Padding(12),ForeColor=muted,Text="吸锁定按接口比例显示；缺失项目显示未记录。"+(b.Mode<20?"1v1 官网项目不同：输出、存活角色、击败觉醒敌人。":"")},45,DockStyle.Bottom));return dialog;
 }
 void ShowBattleDetails(Battle b){using(var dialog=BattleDetailForm(b))dialog.ShowDialog(this);}
}
}
