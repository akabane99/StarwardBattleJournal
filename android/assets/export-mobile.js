(() => {
  'use strict';
  const allowed = new Set(['season_list','season_show','battle_summary','battle_seasonsummary','battle_list','battle_detail1v1','battle_detail2v2','roles','role_list']);
  const language = { 'zh-CN': ['zh-cn','XZY'], 'zh-Hans': ['zh-cn','XZY'], 'zh-HK': ['zh-tw','XZYGAT'], 'zh-Hant': ['zh-tw','XZYGAT'], 'zh-TW': ['zh-tw','XZYGAT'], en:['en-us','XZYSteam'], 'en-US':['en-us','XZYSteam'], ja:['ja-jp','XZYJP'] };
  const selectedRegionEndpoints = new Set(['season_show','battle_summary','battle_seasonsummary','battle_list','roles','role_list']);
  // The official client keeps UI language and selected account region separate.
  function requestInfo(endpoint) {
    const ui=language[localStorage.getItem('i18nextLng')]||language['zh-CN'];
    const selected=localStorage.getItem('xzy_select_lang');
    const region=selectedRegionEndpoints.has(endpoint)&&selected?(language[selected]||language['zh-CN']):ui;
    return {language:ui[0],gameCode:region[1]};
  }
  // Preserve long battle IDs before JSON.parse can round them to JS numbers.
  function lossless(text) {
    let out='', quoted=false, escaped=false;
    for(let i=0;i<text.length;i++) {
      const c=text[i];
      if(quoted) { out+=c; if(escaped)escaped=false;else if(c==='\\')escaped=true;else if(c==='"')quoted=false; continue; }
      if(c==='"'){quoted=true;out+=c;continue;}
      if(c==='-' || /\d/.test(c)) {
        const m=text.slice(i).match(/^-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?/);
        if(m){const num=m[0];out+=/^-?\d{16,}$/.test(num)?JSON.stringify(num):num;i+=num.length-1;continue;}
      }
      out+=c;
    }
    return JSON.parse(out);
  }
  function context() {
    let user={};try{user=JSON.parse(localStorage.getItem('userInfo')||'{}');}catch{}
    return { loggedIn:!!localStorage.getItem('token'), uid:String(localStorage.getItem('selected_role_id')||user.role_id||''), name:String(user.role_name||''), gameCode:requestInfo('battle_list').gameCode };
  }
  async function api(endpoint,query) {
    if(location.origin!=='https://xzy.shengtiangames.com' || !allowed.has(endpoint))throw Error('请求来源或接口不匹配。');
    const target=window.__xzyAutoRefresh;if(target){const c=context();if(!c.loggedIn||c.uid!==String(target.Uid)||c.gameCode.toUpperCase()!==String(target.GameCode).toUpperCase())throw Error('官网账号或区服已改变，刷新已停止，未查询其他账号。');}
    const token=localStorage.getItem('token');
    if(!token && endpoint!=='season_list')throw Error('请先在官方登录页登录并绑定角色。');
    const info=requestInfo(endpoint);
    const headers={lang:info.language,'game-code':info.gameCode};if(token)headers.token=token;
    const url=new URL('/mini-game/xzy/battle-record/'+endpoint,location.origin);
    for(const [key,value]of Object.entries(query||{}))if(value!==null&&value!==undefined)url.searchParams.set(key,String(value));
    const abort=new AbortController();const timer=setTimeout(()=>abort.abort(),25000);
    try {
      const response=await fetch(url,{method:'GET',headers,credentials:'same-origin',signal:abort.signal});
      if(!response.ok){const error=Error('官方接口 HTTP '+response.status);error.code='HTTP '+response.status;error.auth=[401,403].includes(response.status);throw error;}
      return lossless(await response.text());
    }finally{clearTimeout(timer);}
  }
  window.__xzyStatsCollector={context,api,lossless,requestInfo,async dispatch(spec){
    try{const data=spec.kind==='context'?context():await api(spec.endpoint,spec.query);chrome.webview.postMessage({bridge:'xzy-stats',id:spec.id,ok:true,data});}
    catch(e){chrome.webview.postMessage({bridge:'xzy-stats',id:spec.id,ok:false,error:String(e.message||'请求失败')});}
  }};
})();

(async()=>{
  const automatic=window.__xzyAutoRefresh||null;
  const finish=(ok,message,auth=false)=>{if(automatic&&typeof XzyExport.autoFinished==='function'){if(auth)window.__xzyAutoFailedToken=localStorage.getItem('token');XzyExport.autoFinished(ok,message,auth);}};
  if(location.origin!=='https://xzy.shengtiangames.com'||location.pathname!=='/gamerecords_2/dist/index.html'){alert('请先打开星之翼官方战绩网页并登录，再点击此书签。');return;}
  const oldBox=document.getElementById('xzy-season-export-progress');if(oldBox?.dataset.finished==='true')oldBox.remove();else if(oldBox){if(automatic)finish(false,'此网页仍在导出，请等待当前任务完成再刷新。');else alert('正在导出，请等待或停止当前任务。');return;}
  const api=window.__xzyStatsCollector,context=api.context();
  if(!context.loggedIn||!context.uid){if(automatic)finish(false,'请先登录并绑定目标账号。',true);else alert('请先在当前浏览器的官网页面登录并绑定游戏角色，再导出战绩。');return;}
  if(automatic&&(context.uid!==String(automatic.Uid)||context.gameCode.toUpperCase()!==String(automatic.GameCode).toUpperCase())){finish(false,'当前官网账号或区服与刷新目标不同；请选择对应的账号网页。');return;}
  const box=document.createElement('div');box.id='xzy-season-export-progress';box.style.cssText='position:fixed;z-index:2147483647;top:16px;right:16px;width:calc(100vw - 32px);max-width:340px;box-sizing:border-box;padding:18px;background:#fff;color:#173052;border:2px solid #2870c8;border-radius:12px;box-shadow:0 8px 30px #0004;font:14px/1.7 sans-serif';
  const title=document.createElement('strong');title.textContent='星之翼 · 战绩导出 v1.3';const label=document.createElement('div');label.textContent='正在读取官网赛季列表…';const note=document.createElement('div');note.style.fontSize='12px';note.textContent='按四种模式分别读取，并核对官网天梯汇总。';const stop=document.createElement('button');stop.textContent='停止并导出已取得数据';stop.style.cssText='margin-top:10px;padding:6px 10px;cursor:pointer';box.append(title,label,note,stop);document.body.append(box);
  let cancelled=false,raw=null;stop.onclick=()=>{cancelled=true;label.textContent='正在停止，等待当前只读请求结束…';};
  const pause=ms=>new Promise(r=>setTimeout(r,ms));
  const call=async(endpoint,query,retries=1)=>{let last;for(let i=0;i<retries;i++){if(cancelled)throw Error('已停止导出。');try{await pause(220);const r=await api.api(endpoint,query);if(Number(r.code)!==0){const err=Error(r.msg||'官方接口暂不可用');err.code=r.code;err.auth=[401,402,600].includes(Number(r.code));throw err;}return r.data;}catch(e){if(!e.endpoint){const info=api.requestInfo(endpoint);e.endpoint=endpoint;e.message=endpoint+' ['+(e.code??'请求失败')+'，'+info.gameCode+'，'+info.language+']：'+String(e.message||'读取失败');}last=e;if(e.auth)throw e;if(i+1<retries)await pause(600*(i+1));}}throw last;};
  const download=()=>{const error=XzyExport.receiveExport(JSON.stringify(raw));if(typeof error==='string'&&error)throw Error(error);};
  try{
    const now=Math.floor(Date.now()/1000),seasons=await call('season_list');if(!Array.isArray(seasons))throw Error('官网赛季列表结构发生变化。');
    const available=seasons.filter(s=>Number(s.StartTime)>0&&Number(s.StartTime)<=now&&Number(s.EndTime)>Number(s.StartTime)).sort((a,b)=>Number(b.StartTime)-Number(a.StartTime));if(!available.length)throw Error('官网未提供可导出的赛季，请检查电脑日期。');
    const current=available.find(s=>now<Number(s.EndTime));
    const picker=document.createElement('select');picker.style.cssText='padding:7px;width:205px;margin-right:8px';for(const s of available){const option=document.createElement('option');option.value=String(s.Code);option.textContent=s.Name+(s===current?'（当前赛季）':'（历史赛季）');picker.append(option);}picker.value=String((current||available[0]).Code);
    const begin=document.createElement('button');begin.textContent='开始导出';begin.style.cssText='padding:7px;cursor:pointer';const controls=document.createElement('div');controls.style.cssText='margin:8px 0';label.textContent='请选择要导出的赛季，再点击“开始导出”。';stop.textContent='取消';
    const season=automatic?available.find(s=>Number(s.Code)===Number(automatic.SeasonCode)):await new Promise(resolve=>{begin.onclick=()=>resolve(available.find(s=>String(s.Code)===picker.value));stop.onclick=()=>{cancelled=true;resolve(null);};controls.append(picker,begin);box.insertBefore(controls,note);});
    if(automatic&&!season)throw Error('所选赛季不在官网可查询列表中。');
    if(!season||cancelled)throw Error('已取消导出。');picker.disabled=begin.disabled=true;stop.textContent='停止并导出已取得数据';stop.onclick=()=>{cancelled=true;label.textContent='正在停止，等待当前只读请求结束…';};
    raw={Format:'XZY-RawBattleStats',Version:1,Uid:context.uid,Name:context.name,GameCode:context.gameCode,SeasonCode:Number(season.Code),SeasonName:season.Name,SeasonStart:Number(season.StartTime),SeasonEnd:Number(season.EndTime),DataAsOf:now,ListComplete:false,Warnings:[],OfficialSummary:null,Roles:null,Matches:[]};
    const knownData=typeof XzyExport.knownRecords==='function'?JSON.parse(XzyExport.knownRecords(context.gameCode,context.uid,Number(season.Code))):{Enabled:false,Records:[]};
    if(automatic&&!knownData.Enabled)throw Error('自动刷新需要增量对比组件，请更新 APK 后重试。');
    if(!Array.isArray(knownData.Records))throw Error('已导入记录索引无法识别，请关闭增量获取后重试。');
    const known=new Map(knownData.Records.map(r=>[String(r.Id),r]));raw.Incremental={Enabled:!!knownData.Enabled,Compared:known.size,Skipped:0,NewRecords:0,RetriedDetails:0};
    const sameRow=(k,r)=>k.Mode===Number(r.BattleType)&&k.Start===Number(r.StartTime)&&[true,false,1,0,'1','0','true','false'].includes(r.BattleResult)&&k.Result===([true,1,'1','true'].includes(r.BattleResult)?'win':'loss')&&k.Damage===Number(r.TotalDamage||0)&&k.Kills===Number(r.BeatHeroNum||0)&&k.Deaths===Number(r.DeathHeroNum||0)&&k.Mvp===[true,1,'1','true'].includes(r.IsMvp);
    const cacheKey=context.gameCode+'|'+context.uid+'|'+season.Code;
    const previous=window.__xzyBattleExportMemory;
    const cache=previous?.key===cacheKey?previous.matches:new Map();window.__xzyBattleExportMemory={key:cacheKey,matches:cache};
    const listedMatches=[],seen=new Set(),modeNames={11:'1v1 匹配',12:'1v1 天梯',21:'2v2 匹配',22:'2v2 天梯'},rangeEnd=Math.min(now,Number(season.EndTime));
    raw.ListAudit={Version:1,Strategy:'per-mode-until-empty-plus-earlier-range',Modes:[11,12,21,22].map(Mode=>({Mode,Pages:[],EarlierProbes:[],EmptyPageReached:false}))};
    const query=(mode,end,page)=>({role_id:context.uid,season_code:season.Code,match_type:String(mode),time_start:season.StartTime,time_end:end,page});
    const modeRows=mode=>listedMatches.filter(m=>Number(m.Row.BattleType)===mode);
    const readRange=async(mode,end,audit,first=null)=>{
      for(let page=1;page<=10000;page++){
        label.textContent='读取 '+season.Name+' / '+modeNames[mode]+' 第 '+page+' 页；发现 '+listedMatches.length+' 场，待获取 '+raw.Matches.length+' 场。';
        const list=page===1&&first!==null?first:await call('battle_list',query(mode,end,page),3);if(!Array.isArray(list))throw Error('官方列表结构发生变化。');
        const trace={Page:page,TimeEnd:end,Count:list.length,Added:0};audit.Pages.push(trace);
        if(!list.length)return true;
        for(const row of list){
          const id=String(row.BattleId??'');if(!id)throw Error('官网返回了缺少 ID 的对局。');
          if(Number(row.BattleType)!==mode)throw Error('官网未按 '+modeNames[mode]+' 筛选，已停止以免混入其他模式。');
          if(Number(row.StartTime)<Number(season.StartTime)||Number(row.StartTime)>end)throw Error('官网返回了查询时间之外的记录，已停止。');
          if(seen.has(id))continue;seen.add(id);trace.Added++;listedMatches.push({Row:row});
          const existing=known.get(id);if(knownData.Enabled&&existing&&existing.Complete&&sameRow(existing,row)){raw.Incremental.Skipped++;continue;}
          if(knownData.Enabled&&existing)raw.Incremental.RetriedDetails++;else raw.Incremental.NewRecords++;
          row.BattleId=id;const old=existing&&!existing.Complete?null:cache.get(id);const reuse=old?.Detail&&['BattleType','BattleResult','StartTime','TotalDamage','BeatHeroNum','DeathHeroNum'].every(k=>String(old.Row[k])===String(row[k]));
          raw.Matches.push({Row:row,Detail:reuse?old.Detail:null,Error:''});
        }
        if(!trace.Added)throw Error('官网重复返回同一页，列表尚未完整。');
      }
      throw Error('列表达到分页上限，当前数据不完整。');
    };
    for(const audit of raw.ListAudit.Modes)audit.EmptyPageReached=await readRange(audit.Mode,rangeEnd,audit);
    raw.ListComplete=raw.ListAudit.Modes.every(a=>a.EmptyPageReached);
    // These enrich the export; their failure must not discard a valid battle list.
    try{raw.OfficialSummary=await call('battle_seasonsummary',{role_id:context.uid,season_code:season.Code});}catch(e){if(cancelled)throw e;raw.Warnings.push('官方赛季汇总未取得，统计以逐场记录为准。'+e.message);}
    try{raw.Roles=await call('roles');}catch(e){if(cancelled)throw e;raw.Warnings.push('角色目录未取得，将尝试使用详情中的角色字段。'+e.message);}
    // Check before the oldest returned row; this can also bypass a range/result cap.
    for(const audit of raw.ListAudit.Modes){
      for(let attempt=0;attempt<20;attempt++){
        const rows=modeRows(audit.Mode);if(!rows.length)break;const earliest=Math.min(...rows.map(m=>Number(m.Row.StartTime)));if(earliest<=Number(season.StartTime))break;
        const end=earliest-1;label.textContent='复核 '+modeNames[audit.Mode]+' 较早时间段；已取得 '+rows.length+' 场。';
        try{
          const older=await call('battle_list',query(audit.Mode,end,1),3);if(!Array.isArray(older))throw Error('较早时间段列表结构发生变化。');
          audit.EarlierProbes.push({TimeStart:Number(season.StartTime),TimeEnd:end,FirstPageCount:older.length});
          if(!older.length)break;
          if(older.some(r=>Number(r.StartTime)>end||Number(r.StartTime)<Number(season.StartTime))){raw.Warnings.push('较早时间段复核：官网未遵循时间筛选，未合并这些返回记录。');break;}
          await readRange(audit.Mode,end,audit,older);if(attempt===19)raw.Warnings.push('较早时间段补查达到次数上限，仍不能确认整季覆盖。');
        }catch(e){if(cancelled||e.auth)throw e;raw.Warnings.push('较早时间段复核未完成：'+e.message);break;}
      }
      const rows=modeRows(audit.Mode);audit.Count=rows.length;audit.FirstTime=rows.length?Math.min(...rows.map(m=>Number(m.Row.StartTime))):null;audit.LastTime=rows.length?Math.max(...rows.map(m=>Number(m.Row.StartTime))):null;
      const total=raw.OfficialSummary?.[audit.Mode===12?'T1':audit.Mode===22?'T2':'']?.TotalCnt;audit.OfficialCount=total===undefined?null:Number(total);
      if(audit.OfficialCount!==null&&Number.isFinite(audit.OfficialCount)&&audit.Count!==audit.OfficialCount)raw.Warnings.push('[场数核对] '+modeNames[audit.Mode]+'：官网汇总 '+audit.OfficialCount+' 场，逐场列表 '+audit.Count+' 场，相差 '+Math.abs(audit.Count-audit.OfficialCount)+' 场。分页结束不代表整季记录齐全；角色、队伍和队友统计仅使用取得的逐场记录。');
    }
    let missing=0,done=0;const todo=raw.Matches.filter(m=>!m.Detail);
    for(const match of todo){label.textContent='列表 '+raw.Matches.length+' 场；正在补齐角色详情 '+(++done)+' / '+todo.length+'。';try{match.Detail=await call(Number(match.Row.BattleType)<20?'battle_detail1v1':'battle_detail2v2',{id:match.Row.BattleId},3);cache.set(match.Row.BattleId,match);}catch(e){match.Error=String(e.message||'读取详情失败');missing++;if(e.auth||cancelled)throw e;}}
    if(missing)raw.Warnings.push(missing+' 场角色详情暂未取得；这些场次仍计入总战绩。');if(raw.Incremental.Enabled)raw.Warnings.push('增量获取：对比已导入 '+raw.Incremental.Compared+' 场，跳过完整对局 '+raw.Incremental.Skipped+' 场，新记录 '+raw.Incremental.NewRecords+' 场，重试已有详情 '+raw.Incremental.RetriedDetails+' 场。本次列表仍完整查询，用于发现漏记；累计覆盖以合并后核对为准。');download();label.textContent=(raw.Incremental.Enabled?'增量完成：新增 '+raw.Incremental.NewRecords+' 场，跳过 '+raw.Incremental.Skipped+' 场，详情重试 '+raw.Incremental.RetriedDetails+' 场。':'已导出 '+raw.Matches.length+' 场 '+season.Name+' 逐场记录。')+(raw.Warnings.some(w=>w.startsWith('[场数核对]'))?'场数与官网汇总不同，请查看程序中的缺口提示。':'已传回分析程序，点击顶部返回分析。');box.dataset.finished='true';stop.textContent='关闭提示';stop.onclick=()=>box.remove();finish(true,season.Name+'：新增 '+raw.Incremental.NewRecords+' 场，跳过 '+raw.Incremental.Skipped+' 场，补查详情 '+raw.Incremental.RetriedDetails+' 场。'+(missing?'仍有 '+missing+' 场详情待补齐。':''));
  }catch(e){if(raw&&raw.Matches.length){raw.Warnings.push('导出未完成：'+String(e.message||'接口读取失败'));try{download();}catch(saveError){e=saveError;}label.textContent='已导出当前取得的 '+raw.Matches.length+' 场；'+String(e.message||'读取失败')+' 请注意数据覆盖提示。';}else label.textContent='未导出：'+String(e.message||'读取失败');if(e.auth)note.textContent=Number(e.code)===402?'官网要求重新绑定角色。请在官网完成绑定后再次点击书签。':'官网拒绝了当前登录状态。请刷新官网确认战绩能重新加载；如弹出登录框，重新登录后再点击此书签。页面上保留的旧战绩不代表登录仍有效。';box.dataset.finished='true';stop.textContent='关闭提示';stop.onclick=()=>box.remove();finish(false,String(e.message||'刷新失败'),!!e.auth);}
})();
