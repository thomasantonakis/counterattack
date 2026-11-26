using System;
using System.Collections.Generic;
using UnityEngine;
public partial class CreateNewGameManager
{
    private Action<GameSettings> onlineSetupConfirmation;
    public void BindOnlineSetupConfirmation(Action<GameSettings> submit)
        =>onlineSetupConfirmation=submit??throw new ArgumentNullException(nameof(submit));
    public void UnbindOnlineSetupConfirmation()=>onlineSetupConfirmation=null;
    public GameSettings CaptureOnlineSetupDraft()=>CaptureSettingsFromUi(GetHomeTeamNameForSettings(),GetAwayTeamNameForSettings(),false);
    public void ApplyOnlineSetupSettings(GameSettings settings, bool? ownsHome = null)
    {
        var localName = ownsHome == true ? homeTeamInputField : ownsHome == false ? awayTeamInputField : null;
        bool sameRegularMode = !IsInternationalMatchSelected() && !IsInternationalSettings(settings);
        ApplyExistingSettingsToUi(settings, sameRegularMode && localName != null && localName.isFocused ? localName : null);
    }
    private void RefreshOwnedInternationalKits(TMPro.TMP_Dropdown changedTeam)
    {
        bool home = changedTeam == homeInternationalTeamDropdown;
        var localKit = home ? homeKitDropdown : awayKitDropdown;
        var localKeeper = home ? homeGKKitDropdown : awayGKKitDropdown;
        var remoteKit = GetSelectedKitPreset(home ? awayKitDropdown : homeKitDropdown);
        var remoteKeeper = GetSelectedKitPreset(home ? awayGKKitDropdown : homeGKKitDropdown);
        RefreshInternationalTeamKitOptions();

        var kits = GetPresetListForDropdown(localKit);
        foreach (var kit in kits)
        {
            var homeKit = home ? kit : remoteKit;
            var awayKit = home ? remoteKit : kit;
            var keeper = FindCompatibleGoalkeeperKit(homeKit, awayKit, remoteKeeper);
            if (keeper == null) continue;
            var similarity = TokenKitCatalog.GetSimilarityBreakdown(homeKit?.Id, awayKit?.Id);
            if (!string.IsNullOrEmpty(ValidateSelectedKits(homeKit, awayKit, home ? keeper : remoteKeeper,
                home ? remoteKeeper : keeper, similarity))) continue;
            PopulateKitDropdown(localKit, kit.Id);
            SetKitDropdownSelectionWithoutNotify(localKeeper, keeper.Id);
            return;
        }

        PopulateKitDropdown(localKit, GetPreferredInternationalKitId(GetInternationalTeamKitOptions(
            GetSelectedInternationalTeamName(changedTeam))));
    }

    private bool RouteOnlineSetupConfirmation()
    {
        if(onlineSetupConfirmation==null)return false;
        if(TryCaptureOnlineSetupSettings(out var settings,out var error))onlineSetupConfirmation(settings);
        else Debug.LogWarning("Online setup confirmation blocked: "+error);
        return true;
    }
    // Snapshot only: no save paths, scene changes or guest referee randomization.
    public bool TryCaptureOnlineSetupSettings(out GameSettings settings,out string error)
    {
        settings=null;
        if(!ValidateOnlineSetupSelection(out error))return false;
        settings=CaptureSettingsFromUi(GetHomeTeamNameForSettings(),GetAwayTeamNameForSettings(),false);return true;
    }
    public bool ValidateOnlineSetupSelection(out string error)
    {
        if(!TryGetValidatedTeamNames(out _,out _,out error))return false;
        error=GetKitValidationMessage(TokenKitCatalog.GetSimilarityBreakdown(GetSelectedKitPresetId(homeKitDropdown),GetSelectedKitPresetId(awayKitDropdown)));
        return string.IsNullOrEmpty(error);
    }
    private GameSettings CaptureSettingsFromUi(string homeTeamName,string awayTeamName,bool finalizeForLocalSave)
    {
        // Create a GameSettings object and populate it from the UI input fields
        GameSettings settings = new GameSettings();
        settings.gameMode = GetSelectedGameModeForSettings();
        settings.halfDuration = (int)halfDurationSlider.value;
        settings.numberOfHalfs = int.Parse(numberOfHalvesDropdown.options[numberOfHalvesDropdown.value].text);
        if(finalizeForLocalSave)RefreshTiebreakerOptions();
        settings.tiebreaker = tiebreakerDropdown.options[tiebreakerDropdown.value].text;
        settings.matchType = matchTypeDropdown.options[matchTypeDropdown.value].text;
        settings.squadSize = squadSizeDropdown.options[squadSizeDropdown.value].text;
        settings.draft = draftDropdown.options[draftDropdown.value].text;
        settings.gkDraft = gkDraftDropdown.options[gkDraftDropdown.value].text;
        string selectedReferee = refereeDropdown.options[refereeDropdown.value].text;
        // Check if the selected referee is "Random"
        if (selectedReferee == "Random" && finalizeForLocalSave)
        {
            // Create a list of referees excluding "Random"
            List<string> nonRandomReferees = new List<string> { "Webster - 2", "Castolo - 3", "Bakker - 4", "Read - 5" };
            // Randomly select one of the non-random referees
            System.Random rng = new System.Random();
            selectedReferee = nonRandomReferees[rng.Next(nonRandomReferees.Count)];
            Debug.Log($"Random referee chosen: {selectedReferee}");
        }
        settings.referee = selectedReferee;
        settings.playerAssistance = GetSelectedPlayerAssistanceValue();
        settings.aiDifficulty = aiDifficultyDropdown != null && aiDifficultyDropdown.options.Count > 0
            ? aiDifficultyDropdown.options[aiDifficultyDropdown.value].text
            : AIDifficultyUltraHardLabel;
        settings.weatherConditions = weatherDropdown.options[weatherDropdown.value].text;
        settings.fixtureLighting = FixtureLightingOptions.FromIndex(fixtureLightingDropdown != null ? fixtureLightingDropdown.value : 0);
        settings.ballColor = ballColorDropdown.options[ballColorDropdown.value].text;
        settings.homeTeamName = homeTeamName;
        settings.awayTeamName = awayTeamName;
        RefreshTeamControlValidity(null);
        bool homeCpuControlled = IsSinglePlayerCreateMode() && IsCpuControlled(homeTeamControlDropdown);
        bool awayCpuControlled = IsSinglePlayerCreateMode() && IsCpuControlled(awayTeamControlDropdown);
        settings.homeTeamControl = IsSinglePlayerCreateMode() ? GetTeamControlText(homeTeamControlDropdown) : TeamControlHumanLabel;
        settings.awayTeamControl = IsSinglePlayerCreateMode() ? GetTeamControlText(awayTeamControlDropdown) : TeamControlHumanLabel;
        bool usesRegularDraft = string.Equals(settings.draft, "Regular", StringComparison.OrdinalIgnoreCase);
        settings.homeDraftPersona = ShouldApplyCpuDraftPersona(homeCpuControlled, usesRegularDraft) ? DraftProfileSophisticated : string.Empty;
        settings.awayDraftPersona = ShouldApplyCpuDraftPersona(awayCpuControlled, usesRegularDraft) ? DraftProfileSophisticated : string.Empty;
        settings.defaultDraftPersona = IsSinglePlayerCreateMode() ? string.Empty : DraftProfileSophisticated;
        settings.homeRoomPersona = IsSinglePlayerCreateMode() ? ResolveRoomPersonaFromTeamControl(homeTeamControlDropdown) : RoomPersonaAskHuman;
        settings.awayRoomPersona = IsSinglePlayerCreateMode() ? ResolveRoomPersonaFromTeamControl(awayTeamControlDropdown) : RoomPersonaAskHuman;
        settings.defaultRoomPersona = homeCpuControlled && awayCpuControlled
            ? RoomPersonaCodexTacticianUltraHard
            : RoomPersonaAskHuman;
        settings.includeTabletopia = includeTabletopiaToggle.isOn;
        settings.includeNonTabletopia = includeNonTabletopiaToggle.isOn;
        settings.includeInternationals = includeInternationalsToggle.isOn;
        settings.includeTabletopiaGK = includeTabletopiaGKToggle.isOn;
        settings.includeNonTabletopiaGK = includeNonTabletopiaGKToggle.isOn;
        settings.includeInternationalsGK = includeInternationalsGKToggle.isOn;
        TokenKitPreset selectedHomeKit = GetSelectedKitPreset(homeKitDropdown);
        TokenKitPreset selectedAwayKit = GetSelectedKitPreset(awayKitDropdown);
        TokenKitPreset selectedHomeGKKit = GetSelectedKitPreset(homeGKKitDropdown);
        TokenKitPreset selectedAwayGKKit = GetSelectedKitPreset(awayGKKitDropdown);
        settings.homeKit = (finalizeForLocalSave ? selectedHomeKit?.DisplayName : selectedHomeKit?.Id) ?? GetSelectedKitPresetId(homeKitDropdown);
        settings.awayKit = (finalizeForLocalSave ? selectedAwayKit?.DisplayName : selectedAwayKit?.Id) ?? GetSelectedKitPresetId(awayKitDropdown);
        settings.homeGKKit = (finalizeForLocalSave ? selectedHomeGKKit?.DisplayName : selectedHomeGKKit?.Id) ?? GetSelectedKitPresetId(homeGKKitDropdown);
        settings.awayGKKit = (finalizeForLocalSave ? selectedAwayGKKit?.DisplayName : selectedAwayGKKit?.Id) ?? GetSelectedKitPresetId(awayGKKitDropdown);
        if (finalizeForLocalSave)
            Debug.Log($"[CreateNewGameManager] Selected kits: Home '{settings.homeKit}' (id '{selectedHomeKit?.Id ?? string.Empty}'), Away '{settings.awayKit}' (id '{selectedAwayKit?.Id ?? string.Empty}'), Home GK '{settings.homeGKKit}' (id '{selectedHomeGKKit?.Id ?? string.Empty}'), Away GK '{settings.awayGKKit}' (id '{selectedAwayGKKit?.Id ?? string.Empty}').");

        return settings;
    }
}
