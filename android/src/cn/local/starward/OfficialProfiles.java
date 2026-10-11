package cn.local.starward;
import android.app.*;
import android.content.Intent;
import android.os.Build;
import android.webkit.WebView;
import android.content.Context;
import android.util.AtomicFile;
import org.json.*;
import java.io.*;
import java.nio.charset.StandardCharsets;

public final class OfficialProfiles extends Application {
    @Override public void onCreate(){super.onCreate();if(Build.VERSION.SDK_INT>=28){String process=Application.getProcessName();int colon=process.indexOf(':');if(colon>=0)WebView.setDataDirectorySuffix(process.substring(colon+1));}}
    public static int count(){return Build.VERSION.SDK_INT>=28?8:1;}
    public static void choose(Activity owner){String[] names=new String[count()];for(int i=0;i<names.length;i++)names[i]="账号网页 "+(i+1);new AlertDialog.Builder(owner).setTitle("官网账号网页（独立登录）").setItems(names,(dialog,index)->open(owner,index+1)).setNegativeButton("取消",null).setMessage(null).show();}
    public static void open(Activity owner,int slot){Class<?>[] classes={OfficialActivity.class,Page2.class,Page3.class,Page4.class,Page5.class,Page6.class,Page7.class,Page8.class};if(slot<1||slot>count())throw new IllegalArgumentException("账号网页编号无效");Intent intent=new Intent(owner,classes[slot-1]);intent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK|Intent.FLAG_ACTIVITY_REORDER_TO_FRONT);owner.startActivity(intent);}
    public static void analysis(Activity owner){Intent intent=new Intent(owner,BattleActivity.class);intent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK|Intent.FLAG_ACTIVITY_CLEAR_TOP|Intent.FLAG_ACTIVITY_SINGLE_TOP);owner.startActivity(intent);}
    private static Class<?> pageClass(int slot){Class<?>[] classes={OfficialActivity.class,Page2.class,Page3.class,Page4.class,Page5.class,Page6.class,Page7.class,Page8.class};return classes[slot-1];}
    private static AtomicFile identityFile(Context c,int slot){return new AtomicFile(new File(c.getFilesDir(),"official-account-"+slot+".json"));}
    private static JSONObject identity(Context c,int slot){try(InputStream in=identityFile(c,slot).openRead();ByteArrayOutputStream out=new ByteArrayOutputStream()){byte[] bytes=new byte[4096];int n;while((n=in.read(bytes))!=-1){if(out.size()+n>16384)return new JSONObject();out.write(bytes,0,n);}return new JSONObject(new String(out.toByteArray(),StandardCharsets.UTF_8));}catch(Exception e){return new JSONObject();}}
    public static void remember(Context c,int slot,JSONObject raw)throws IOException{FileOutputStream out=null;AtomicFile file=identityFile(c,slot);try{JSONObject value=new JSONObject();for(String key:new String[]{"Uid","GameCode","Name"})value.put(key,raw.optString(key));out=file.startWrite();out.write(value.toString().getBytes(StandardCharsets.UTF_8));file.finishWrite(out);}catch(Exception e){if(out!=null)file.failWrite(out);throw new IOException(e);}}
    public static void refresh(Activity owner,String spec){try{JSONObject target=new JSONObject(spec);int slot=1;for(int i=1;i<=count();i++){JSONObject p=identity(owner,i);if(target.getString("Uid").equals(p.optString("Uid"))&&target.getString("GameCode").equalsIgnoreCase(p.optString("GameCode"))){slot=i;break;}}openRefresh(owner,slot,spec);}catch(JSONException e){throw new IllegalArgumentException("刷新目标无效",e);}}
    public static void openRefresh(Activity owner,int slot,String spec){Intent intent=new Intent(owner,pageClass(slot));intent.putExtra("auto-refresh",spec);intent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK|Intent.FLAG_ACTIVITY_CLEAR_TOP|Intent.FLAG_ACTIVITY_SINGLE_TOP);owner.startActivity(intent);}
    public static void chooseRefresh(Activity owner,String spec){String[] names=new String[count()];for(int i=0;i<names.length;i++){String name=identity(owner,i+1).optString("Name");names[i]="账号网页 "+(i+1)+(name.isEmpty()?"":" · "+name);}new AlertDialog.Builder(owner).setTitle("选择刷新目标的登录网页").setItems(names,(d,index)->{if(owner instanceof OfficialActivity)((OfficialActivity)owner).transferAuto();openRefresh(owner,index+1,spec);}).setNegativeButton("取消",null).show();}
    public static void analysis(Activity owner,JSONObject result){Intent intent=new Intent(owner,BattleActivity.class);intent.putExtra("battle-refresh-result",result.toString());intent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK|Intent.FLAG_ACTIVITY_CLEAR_TOP|Intent.FLAG_ACTIVITY_SINGLE_TOP);owner.startActivity(intent);}
    public static class Page2 extends OfficialActivity{}
    public static class Page3 extends OfficialActivity{}
    public static class Page4 extends OfficialActivity{}
    public static class Page5 extends OfficialActivity{}
    public static class Page6 extends OfficialActivity{}
    public static class Page7 extends OfficialActivity{}
    public static class Page8 extends OfficialActivity{}
}
