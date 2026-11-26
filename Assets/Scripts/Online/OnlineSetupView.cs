using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
namespace CounterAttack.Online
{
    // Scene-authored view. The persistent authenticated session supplies its client.
    public sealed class OnlineSetupView : MonoBehaviour
    {
        public CreateNewGameManager setup;
        public TMP_Text status;
        private OnlineSetupClient client;
        private readonly List<Action> release=new();
        private bool applying;
        private long renderedRevision=-1;
        private bool duplicateNationalTeam;
        public void Configure(OnlineSetupClient owner)
        {
            Unbind();client=owner??throw new ArgumentNullException(nameof(owner));
            if(setup==null)throw new InvalidOperationException("Online setup requires the authored Create New Game manager");
            if(status!=null)status.gameObject.SetActive(true);
            setup.BindOnlineSetupConfirmation(Confirm);
            client.Changed+=Render;release.Add(()=>client.Changed-=Render);
            foreach(var field in new[]{setup.homeTeamInputField,setup.awayTeamInputField})
                if(field!=null){UnityAction<string> listener=_=>Edited();field.onValueChanged.AddListener(listener);release.Add(()=>field.onValueChanged.RemoveListener(listener));}
            foreach(var field in Dropdowns())
                if(field!=null){UnityAction<int> listener=_=>Edited();field.onValueChanged.AddListener(listener);release.Add(()=>field.onValueChanged.RemoveListener(listener));}
            foreach(var field in new[]{setup.halfDurationSlider,setup.playerAssistanceSlider})
                if(field!=null){UnityAction<float> listener=_=>Edited();field.onValueChanged.AddListener(listener);release.Add(()=>field.onValueChanged.RemoveListener(listener));}
            foreach(var field in Pools())
                if(field!=null){UnityAction<bool> listener=_=>Edited();field.onValueChanged.AddListener(listener);release.Add(()=>field.onValueChanged.RemoveListener(listener));}
            Render();
        }
        public void RefreshSelections(){renderedRevision=-1;Render();}
        private void Confirm(GameSettings selections)
        {
            if (client.Confirm(selections)) MenuAudioPlayer.Play(GameplayAudioPool.MenuAdvance);
            else if (status != null) status.text = "Review your selections before confirming.";
        }
        private void Edited()
        {
            if(applying || client==null || setup.IsApplyingMatchType)return;
            duplicateNationalTeam = false;
            var proposed = setup.CaptureOnlineSetupDraft();
            if (proposed.matchType == "International" && proposed.homeTeamName == proposed.awayTeamName
                && client.Capture()?.settings.matchType == "International")
            {
                duplicateNationalTeam = true;
                RefreshSelections();
                return;
            }
            client.Edit(proposed);
        }
        private IEnumerable<TMP_Dropdown> Dropdowns()=>new[]{setup.gameModeDropdown,setup.numberOfHalvesDropdown,setup.tiebreakerDropdown,
            setup.playerAssistanceDropdown,setup.matchTypeDropdown,setup.squadSizeDropdown,setup.draftDropdown,setup.gkDraftDropdown,setup.refereeDropdown,
            setup.weatherDropdown,setup.fixtureLightingDropdown,setup.ballColorDropdown,setup.homeKitDropdown,setup.awayKitDropdown,
            setup.homeGKKitDropdown,setup.awayGKKitDropdown,setup.homeInternationalTeamDropdown,setup.awayInternationalTeamDropdown};
        private IEnumerable<Toggle> Pools()=>new[]{setup.includeTabletopiaToggle,setup.includeNonTabletopiaToggle,setup.includeInternationalsToggle,
            setup.includeTabletopiaGKToggle,setup.includeNonTabletopiaGKToggle,setup.includeInternationalsGKToggle};
        private void Render()
        {
            if(client==null)return;var state=client.Capture();
            if(state!=null && state.revision!=renderedRevision)
            {
                applying=true;
                try{setup.ApplyOnlineSetupSettings(client.ViewSettings(), client.Team==OnlineTeam.Home);renderedRevision=state.revision;}
                finally{applying=false;}
            }
            RefreshControls();
        }
        private void LateUpdate(){if(client!=null)RefreshControls();}
        private static void Interactable(Selectable control,bool value){if(control!=null)control.interactable=value;}
        private void RefreshControls()
        {
            var state=client.Capture();bool editing=state?.phase=="setup",home=client.Team==OnlineTeam.Home,shared=editing && client.IsHost;
            foreach(var field in Dropdowns())Interactable(field,shared);
            foreach(var field in Pools())Interactable(field,shared && state?.settings.matchType!="International");
            Interactable(setup.halfDurationSlider,shared);Interactable(setup.playerAssistanceSlider,shared);
            Interactable(setup.gameModeDropdown,false);Interactable(setup.homeTeamControlDropdown,false);Interactable(setup.awayTeamControlDropdown,false);
            Interactable(setup.homeTeamInputField,editing && home);Interactable(setup.awayTeamInputField,editing && !home);
            Interactable(setup.homeKitDropdown,editing && home);Interactable(setup.homeGKKitDropdown,editing && home);
            Interactable(setup.awayKitDropdown,editing && !home);Interactable(setup.awayGKKitDropdown,editing && !home);
            Interactable(setup.homeInternationalTeamDropdown,editing && home);Interactable(setup.awayInternationalTeamDropdown,editing && !home);
            if(state?.settings.matchType=="International"){Interactable(setup.draftDropdown,false);Interactable(setup.gkDraftDropdown,false);}
            bool valid=setup.ValidateOnlineSetupSelection(out var error);
            Interactable(setup.createGameButton,client.CanConfirm && valid);
            var label=setup.createGameButton?.GetComponentInChildren<TMP_Text>(true);
            if(label!=null)label.text=client.IsHost && client.BothReady && !client.Dirty?"Start Draft":client.LocalReady && !client.Dirty?"Waiting for other manager":"Confirm selections";
            if(status==null)return;
            status.text=editing && duplicateNationalTeam?"Choose a different national team.":editing && !valid?error:state?.phase=="draft-ready"?"Both managers agreed. Preparing Draft...":!client.Connected?"Waiting for the other manager.":client.Pending?"Sending your selection..."
                :client.LastReply is OnlineCommandStatus.IllegalAction or OnlineCommandStatus.StaleRevision?"Selections changed. Review and confirm again."
                :client.BothReady?client.IsHost?"Both managers agreed. You can start Draft.":"Both managers agreed. Waiting for the host to start Draft."
                :client.LocalReady && !client.Dirty?"Your selections are confirmed. Waiting for the other manager.":"Review your team name and kits, then confirm your selections.";
        }
        public void Unbind()
        {
            foreach(var action in release)action();release.Clear();
            if(client!=null && setup!=null)setup.UnbindOnlineSetupConfirmation();
            if(client!=null && status!=null)status.gameObject.SetActive(false);
            client=null;renderedRevision=-1;duplicateNationalTeam=false;
        }
        private void OnDestroy()=>Unbind();
    }
}
