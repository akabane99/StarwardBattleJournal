package cn.local.starward;
import android.content.Context;
import android.util.AtomicFile;
import org.json.*;
import java.io.*;
import java.net.*;
import java.nio.charset.StandardCharsets;
import java.util.*;
import javax.net.ssl.HttpsURLConnection;

final class LeaderboardService {
 private static final int LIMIT=8*1024*1024;
 static String read(InputStream input)throws IOException {try(InputStream in=input;ByteArrayOutputStream out=new ByteArrayOutputStream()){byte[] buffer=new byte[8192];int n;while((n=in.read(buffer))!=-1){if(out.size()+n>LIMIT)throw new IOException("排行榜数据超过 8 MB");out.write(buffer,0,n);}return new String(out.toByteArray(),StandardCharsets.UTF_8);}}
 static String fetch(String endpoint,String query)throws Exception {
  if(!Arrays.asList("season_list","ranking_list_new").contains(endpoint))throw new IOException("不支持的排行榜接口");
  JSONObject q=new JSONObject(query);StringBuilder address=new StringBuilder("https://xzy.shengtiangames.com/mini-game/xzy/battle-record/"+endpoint);String game="XZY";
  if("season_list".equals(endpoint)){if(q.length()!=0)throw new IOException("赛季查询不接受参数");}
  else {if(q.length()!=3||!q.has("area_id")||!q.has("banking_type")||!q.has("season_code"))throw new IOException("排行榜参数无效");int area=q.getInt("area_id"),mode=q.getInt("banking_type"),season=q.getInt("season_code");if(!Arrays.asList(101,102,103).contains(area)||!Arrays.asList(1,2).contains(mode)||season<1||season>1000)throw new IOException("赛区、模式或赛季无效");game=area==101?"XZY":area==102?"XZYSteam":"XZYJP";address.append("?area_id=").append(area).append("&banking_type=").append(mode).append("&season_code=").append(season);}
  HttpsURLConnection connection=(HttpsURLConnection)new URL(address.toString()).openConnection();
  try {connection.setConnectTimeout(15000);connection.setReadTimeout(25000);connection.setInstanceFollowRedirects(false);connection.setRequestProperty("lang","zh-cn");connection.setRequestProperty("game-code",game);if(connection.getResponseCode()!=200)throw new IOException("HTTP "+connection.getResponseCode());String text=read(connection.getInputStream());JSONObject result=new JSONObject(text);if(result.getInt("code")!=0)throw new IOException(result.optString("msg","官网读取失败"));return text;}finally{connection.disconnect();}
 }
 private static AtomicFile cache(Context context){return new AtomicFile(new File(context.getFilesDir(),"leaderboards.json"));}
 static synchronized String readCache(Context context){try{AtomicFile file=cache(context);if(file.getBaseFile().exists()||new File(file.getBaseFile()+".bak").exists())return read(file.openRead());}catch(Exception ignored){}return "{\"Format\":\"XZY-LadderCache\",\"Version\":1,\"Seasons\":[],\"Boards\":[]}";}
 static synchronized String saveCache(Context context,String text){AtomicFile file=cache(context);FileOutputStream out=null;try{if(text.getBytes(StandardCharsets.UTF_8).length>LIMIT)throw new IOException("排行榜缓存超过 8 MB");JSONObject data=new JSONObject(text);if(!"XZY-LadderCache".equals(data.getString("Format"))||data.getInt("Version")!=1||data.getJSONArray("Boards").length()>36||data.getJSONArray("Seasons").length()>200)throw new IOException("排行榜缓存格式无效");out=file.startWrite();out.write(text.getBytes(StandardCharsets.UTF_8));file.finishWrite(out);return "";}catch(Exception e){if(out!=null)file.failWrite(out);return e.getMessage();}}
}
