// ── Toast ─────────────────────────────────────────────────────
function toast(msg, type='ok'){
  const w=document.getElementById('toast-wrap');
  const el=document.createElement('div');
  el.className=`toast-item toast-${type}`;
  el.innerHTML=`<span>${type==='ok'?'✓':'✕'}</span><span>${msg}</span>`;
  w.appendChild(el);
  setTimeout(()=>el.remove(),3200);
}

// ── Modal ─────────────────────────────────────────────────────
let _modal=null;
async function openModal(title,url,size=''){
  document.getElementById('modalLabel').textContent=title;
  const dlg=document.getElementById('formModal');
  dlg.querySelector('.modal-dialog').className='modal-dialog modal-dialog-centered'+(size?' '+size:'');
  const body=document.getElementById('modalBody');
  body.innerHTML='<div class="text-center py-4"><div class="spinner-border text-primary" style="width:1.4rem;height:1.4rem"></div></div>';
  _modal=new bootstrap.Modal(dlg);
  _modal.show();
  const res=await fetch(url);
  body.innerHTML=await res.text();
  body.querySelectorAll('script').forEach(oldScript=>{
    const newScript=document.createElement('script');
    Array.from(oldScript.attributes).forEach(a=>newScript.setAttribute(a.name,a.value));
    newScript.textContent=oldScript.textContent;
    oldScript.replaceWith(newScript);
  });
}
function closeModal(){_modal?.hide();}

// ── AJAX Form submit ──────────────────────────────────────────
async function submitForm(formId,onOk){
  const form=document.getElementById(formId);
  const data=new FormData(form);
  try{
    const res=await fetch(form.action,{method:'POST',body:data});
    const ct=res.headers.get('content-type')||'';
    if(ct.includes('application/json')){
      const j=await res.json();
      if(j.ok){closeModal();toast('Lưu thành công!');if(onOk)onOk();else if(window.quickVatTuSavedCallback){const cb=window.quickVatTuSavedCallback;window.quickVatTuSavedCallback=null;cb();}else location.reload();}
      else toast(j.msg||'Lỗi!','err');
    } else {
      document.getElementById('modalBody').innerHTML=await res.text();
    }
  }catch{toast('Lỗi kết nối.','err');}
}

// ── Confirm + POST ────────────────────────────────────────────
async function confirmAction(msg,url,onOk){
  if(!confirm(msg))return;
  try{
    const res=await fetch(url,{method:'POST',headers:{'RequestVerificationToken':getToken()}});
    const j=await res.json();
    if(j.ok){toast('Thành công!');if(onOk)onOk();else location.reload();}
    else toast(j.msg||'Lỗi!','err');
  }catch{toast('Lỗi kết nối.','err');}
}
async function postJson(url,data,onOk){
  try{
    const form=new FormData();
    Object.entries(data).forEach(([k,v])=>form.append(k,v));
    const res=await fetch(url,{method:'POST',body:form});
    const j=await res.json();
    if(j.ok){toast('Cập nhật thành công!');if(onOk)onOk();else location.reload();}
    else toast(j.msg||'Lỗi!','err');
  }catch{toast('Lỗi.','err');}
}
function getToken(){
  return document.querySelector('input[name="__RequestVerificationToken"]')?.value||'';
}

// ── Datalist helpers ──────────────────────────────────────────
function normalizeDatalistText(s){
  return (s||'').toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g,'').replace(/\s+/g,' ').trim();
}
function findSelectedDatalistOption(input){
  const listId=input?.getAttribute('list');
  if(!listId)return null;
  const list=document.getElementById(listId);
  if(!list)return null;
  const q=normalizeDatalistText(input.value);
  if(!q)return null;
  return Array.from(list.options).find(o=>normalizeDatalistText(o.value)===q) || null;
}
function syncHiddenFromDatalist(input, hiddenId){
  const hidden=document.getElementById(hiddenId);
  if(!hidden)return null;
  const opt=findSelectedDatalistOption(input);
  if(opt){
    hidden.value=opt.dataset.id||'';
    input.value=opt.value;
  }else{
    hidden.value='';
  }
  return opt;
}
function syncKhuonBeVatTu(input){
  const opt=syncHiddenFromDatalist(input,'IdVatTu');
  const ma=document.getElementById('MaKhuon');
  const ten=document.getElementById('TenKhuon');
  if(opt || input?.value){
    const selectedText=opt?.value||input.value||'';
    const maVt=opt?.dataset.ma||((selectedText.match(/^(VT\d+)/i)||[])[1]||'');
    const tenVt=opt?.dataset.ten||selectedText.replace(/^(VT\d+)\s*-\s*/i,'').trim();
    if(ma && maVt) ma.value=maVt;
    if(ten && !ten.value.trim() && tenVt) ten.value=tenVt;
  }
}

document.addEventListener('input', e=>{
  if(e.target?.id==='vatTuKhuonBeSearch') syncKhuonBeVatTu(e.target);
  if(e.target?.id==='vatTuDinhMucSearch') syncHiddenFromDatalist(e.target,'IdVatTu');
});
document.addEventListener('change', e=>{
  if(e.target?.id==='vatTuKhuonBeSearch') syncKhuonBeVatTu(e.target);
  if(e.target?.id==='vatTuDinhMucSearch') syncHiddenFromDatalist(e.target,'IdVatTu');
});

// ── Active nav link ───────────────────────────────────────────
document.addEventListener('DOMContentLoaded',()=>{
  const path=location.pathname.toLowerCase();
  document.querySelectorAll('.nav-link').forEach(a=>{
    const href=(a.getAttribute('href')||'').toLowerCase();
    if(href==='/'&&path==='/'){a.classList.add('active');return;}
    if(href&&href!=='/'&&path.startsWith(href))a.classList.add('active');
  });

  // Mobile sidebar
  document.getElementById('menuToggle')?.addEventListener('click',()=>{
    document.getElementById('sidebar').classList.toggle('open');
  });
});
