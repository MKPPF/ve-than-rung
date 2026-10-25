using TopDownGame.Core;
using TopDownGame.Maps;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TopDownGame.UI
{
    /// <summary>A modal destination picker requires deliberate travel and keeps locked regions inaccessible.</summary>
    public sealed class LobbyTravelUI : MonoBehaviour
    {
        private LobbyPortal portal;
        private GameObject surface;
        private readonly MenuModalFocus focus = new MenuModalFocus();

        public void Open(LobbyPortal owner)
        {
            if (surface != null || owner == null || GameplayState.IsBlockedByOther(this)) return;
            portal=owner;
            surface=new GameObject("TravelDestinationCanvas",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            surface.transform.SetParent(transform,false);
            var canvas=surface.GetComponent<Canvas>();
            canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder=150;
            var scaler=surface.GetComponent<CanvasScaler>();
            scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1000,760);
            scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            var shade=FantasyEquipmentUI.Box(surface.transform,"Dimmer",0,0,1000,760,new Color(0.02f,0.04f,0.08f,0.8f));
            var rect=shade.rectTransform; rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            shade.raycastTarget=true;
            var panel=FantasyEquipmentUI.Frame(surface.transform,"TravelPanel",0,0,650,580,FantasyEquipmentUI.Ink);
            var panelRect=(RectTransform)panel;panelRect.anchorMin=panelRect.anchorMax=panelRect.pivot=Vector2.one*0.5f;panelRect.anchoredPosition=Vector2.zero;
            FantasyEquipmentUI.Text(panel,"Eyebrow","TRUYỀN TỐNG TRẬN",30,22,590,28,16,FantasyEquipmentUI.Gold);
            FantasyEquipmentUI.Text(panel,"Title","Bạn muốn đến đâu?",30,54,590,42,28);
            FantasyEquipmentUI.Text(panel,"Hint","Đi qua cửa ra ở vùng trước để khám phá và mở vùng mới.",30,99,590,40,17,FantasyEquipmentUI.Muted);
            for(int i=0;i<LobbyPortal.Destinations.Length;i++)
            {
                string scene=LobbyPortal.Destinations[i];
                bool available=portal.IsDestinationAvailable(scene);
                MapData data = portal.GetDestinationData(scene);
                string label = data != null ? data.DisplayName : scene;
                string levels = data != null ? "Cấp đề nghị " + data.RecommendedMinLevel + "–" + data.RecommendedMaxLevel : "";
                var button=FantasyEquipmentUI.Button(panel,"Travel_"+scene,
                    label + "\n" + levels + (available ? "\nĐÃ MỞ" : "\nCHƯA MỞ"),30+(i%2)*300,155+(i/2)*103,285,85,
                    ()=>portal.ChooseDestination(scene),available);
                button.interactable=available;
            }
            Button cancel = FantasyEquipmentUI.Button(panel,"Cancel","Ở lại sảnh  •  Esc",30,490,590,54,()=>portal.CancelChoice());
            focus.Capture(surface.transform);
            GameplayState.EnterModal(this);
            cancel.Select();
        }

        private void Update()
        {
            if(portal!=null && !GameplayState.IsBlockedByOther(this) && Keyboard.current!=null && Keyboard.current.escapeKey.wasPressedThisFrame)
                portal.CancelChoice();
        }

        public void Close()
        {
            GameplayState.ExitModal(this);
            if(surface!=null) {surface.SetActive(false); Destroy(surface);}
            focus.Release();
            Destroy(this);
        }
        private void OnDisable()
        {
            GameplayState.ExitModal(this);
            focus.Release();
            if (surface != null) surface.SetActive(false);
        }
        private void OnDestroy()
        {
            GameplayState.ExitModal(this);
            focus.Release();
            if(surface!=null) Destroy(surface);
        }
    }
}
