using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunPreviewDoubleClickInspection(bool goalkeeper)
    {
        Board(1); var views = LoadPresentationViews(); yield return null;
        var inspection = views.GetComponent<RoomInspectionCard>();
        var token = goalkeeper ? homeGK : attacker;
        Component source = goalkeeper ? (Component)views.GetComponentInChildren<GoalkeeperCard>(true) : views.GetComponentInChildren<PlayerCard>(true);
        var preview = Object.Instantiate(source.gameObject, views.transform);
        preview.transform.localScale = Vector3.one * .25f; preview.SetActive(true);
        if (goalkeeper) preview.GetComponent<GoalkeeperCard>().UpdateFromToken(token);
        else preview.GetComponent<PlayerCard>().UpdateFromToken(token);
        var pointer = new UnityEngine.EventSystems.PointerEventData(null) { clickCount = 1 };
        void Click() => UnityEngine.EventSystems.ExecuteEvents.Execute(preview, pointer, UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
        Click(); Assert.That(inspection.IsOpen, Is.False, "Single click does nothing.");
        pointer.clickCount = 2; Click();
        Assert.That(inspection.LockedPlayer, Is.SameAs(token), "Double click inspects the displayed player.");
        Click(); Assert.That(inspection.LockedPlayer, Is.SameAs(token), "An open card stays locked.");
        input.SimulateKeyDataPress(KeyCode.Space); Assert.That(inspection.IsOpen, Is.False);
        match.SetPauseMenuOpen(true); Click(); Assert.That(inspection.IsOpen, Is.False); match.SetPauseMenuOpen(false);
        var modal = Child("Preview modal owner"); GameplayInputConsumer.Ensure(modal, true); modal.SetActive(true);
        Click(); Assert.That(inspection.IsOpen, Is.False, "Existing modals retain priority.");
        modal.SetActive(false);
        pointer.button = UnityEngine.EventSystems.PointerEventData.InputButton.Right; Click();
        Assert.That(inspection.IsOpen, Is.False);
    }

    public IEnumerator RunRoomPreviewIgnoresDraftPointers()
    {
        Board(1); var views = LoadPresentationViews(); yield return null;
        var card = views.GetComponentInChildren<PlayerCard>(true);
        card.gameObject.SetActive(true);
        var handler = card.GetComponent<PlayerCardDragHandler>();
        Assert.That(handler, Is.Not.Null, "Room card copies share the preview card's draft handler.");
        var parent = card.transform.parent;
        var position = card.transform.position;
        int childCount = parent.childCount;
        var pointer = new UnityEngine.EventSystems.PointerEventData(null) { clickCount = 2, position = new Vector2(150f, 250f) };
        UnityEngine.EventSystems.ExecuteEvents.Execute(card.gameObject, pointer, UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
        UnityEngine.EventSystems.ExecuteEvents.Execute(card.gameObject, pointer, UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
        UnityEngine.EventSystems.ExecuteEvents.Execute(card.gameObject, pointer, UnityEngine.EventSystems.ExecuteEvents.beginDragHandler);
        UnityEngine.EventSystems.ExecuteEvents.Execute(card.gameObject, pointer, UnityEngine.EventSystems.ExecuteEvents.dragHandler);
        UnityEngine.EventSystems.ExecuteEvents.Execute(card.gameObject, pointer, UnityEngine.EventSystems.ExecuteEvents.endDragHandler);
        Assert.That(card.transform.parent, Is.SameAs(parent));
        Assert.That(card.transform.position, Is.EqualTo(position));
        Assert.That(parent.childCount, Is.EqualTo(childCount), "Preview interactions create no draft placeholders.");
        yield return null;
        Assert.That(card != null, Is.True);
    }

    public IEnumerator RunRoomShutdownDoesNotRecreateApplication()
    {
        Board(1);
        var previousApp = ApplicationManager.Instance;
        var property = typeof(ApplicationManager).GetProperty(nameof(ApplicationManager.Instance));
        var app = Child("Destroyed application context").AddComponent<ApplicationManager>();
        property.SetValue(null, app);
        Object.DestroyImmediate(app.gameObject);
        try
        {
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, "No active save file is selected for this match.");
            Invoke(match, "OnDestroy");
            Assert.That(ApplicationManager.Instance == null, Is.True, "Teardown logging must not create a replacement ApplicationManager.");
            Assert.That(match.FlushLiveLogNow(out string path, out string message, force: false), Is.False);
            Assert.That(message, Is.EqualTo("No active save file is selected for this match."));
            Assert.That(path, Is.Empty);
            Assert.That(ApplicationManager.Instance == null, Is.True);
        }
        finally
        {
            if (ApplicationManager.Instance != null && ApplicationManager.Instance != previousApp)
                Object.DestroyImmediate(ApplicationManager.Instance.gameObject);
            property.SetValue(null, previousApp);
        }
        yield return null;
    }

    private GameObject LoadPresentationViews()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RoomPresentationEditorTools.TestViewsPath);
        Assert.That(prefab, Is.Not.Null, "Author Room presentation views in Edit Mode before running these tests.");
        var views = Object.Instantiate(prefab, root.transform);
        Set(views.GetComponentInChildren<MovementPhaseMovedTokensPanel>(true), "movementPhaseManager", movement);
        views.SetActive(true);
        return views;
    }

    public IEnumerator RunInspectionCardPresentation(bool goalkeeper, bool lineup)
    {
        Board(1); var views = LoadPresentationViews(); yield return null;
        var inspection = views.GetComponent<RoomInspectionCard>();
        var token = goalkeeper ? homeGK : attacker;
        input.hoveredToken = defender;
        if (lineup) input.SetLineupHoverToken(token); else input.hoveredToken = token;
        input.SimulateKeyDataPress(KeyCode.Space);
        Assert.That(inspection.LockedPlayer, Is.SameAs(token));
        Assert.That(views.GetComponentInChildren<GoalkeeperCard>(true).gameObject.activeSelf, Is.EqualTo(goalkeeper));
        Assert.That(views.GetComponentInChildren<PlayerCard>(true).gameObject.activeSelf, Is.EqualTo(!goalkeeper));
        var card = goalkeeper ? (Component)views.GetComponentInChildren<GoalkeeperCard>(true) : views.GetComponentInChildren<PlayerCard>(true);
        var area = (RectTransform)card.transform.parent;
        Assert.That(area.GetComponent<Canvas>().overrideSorting, Is.True, "Large inspection cards draw above the hover-name Canvas.");
        Assert.That(card.GetComponentsInChildren<UnityEngine.UI.Image>(true).Count(i => i.name.EndsWith("ValueBadge") && i.sprite != null), Is.EqualTo(7), "All seven attribute values retain authored circular backgrounds.");
        foreach (var value in card.GetComponentsInChildren<TMPro.TMP_Text>(true).Where(t => t.name.EndsWith("Value")))
        {
            var badge = value.transform.parent.Find(value.name + "Badge");
            Assert.That(badge.GetSiblingIndex(), Is.LessThan(value.transform.GetSiblingIndex()), "Attribute numbers draw above their circles after repeated authoring.");
            float diameter = ((RectTransform)badge).rect.width;
            Assert.That(diameter, Is.GreaterThan(value.fontSize));
            Assert.That(diameter, Is.LessThan(((RectTransform)value.transform.parent).rect.height), "Circles fit inside their attribute rows.");
        }
        Assert.That(area.anchorMin, Is.EqualTo(new Vector2(.5f, .2f)));
        Assert.That(area.anchorMax, Is.EqualTo(new Vector2(.5f, .8f)));
        yield return null;
        var rect = (RectTransform)card.transform;
        Assert.That(rect.rect.height * rect.localScale.y, Is.EqualTo(area.rect.height).Within(.01f));
        Assert.That(rect.anchoredPosition, Is.EqualTo(Vector2.zero));
        input.hoveredToken = defender; input.SetLineupHoverToken(defender);
        Assert.That(inspection.LockedPlayer, Is.SameAs(token), "Other hover sources cannot replace the locked card.");
        var preset = new KeyPressData(KeyCode.Alpha2, false, false, false);
        Assert.That(inspection.CameraController.TryHandlePresetKey(preset), Is.True);
        Assert.That(inspection.LockedPlayer, Is.SameAs(token));
        var escape = new KeyPressData(KeyCode.Escape, false, false, false);
        Assert.That(RoomInputOwnership.ConsumeKey(escape, true, defender), Is.False, "Pause keeps Escape ownership.");
        var modalObject = Child("Isolated modal input owner");
        GameplayInputConsumer.Ensure(modalObject, true); modalObject.SetActive(true);
        var space = new KeyPressData(KeyCode.Space, false, false, false);
        Assert.That(RoomInputOwnership.ConsumeKey(space, true, defender), Is.False, "Modal dispatch occurs before card dispatch.");
        Assert.That(inspection.LockedPlayer, Is.SameAs(token));
        modalObject.SetActive(false);
        input.SimulateKeyDataPress(KeyCode.Space);
        Assert.That(inspection.IsOpen, Is.False);
    }

    public IEnumerator RunInspectionContinuesMovementAndCpuInput()
    {
        Board(1); Subscribe(movement);
        var runner = Token("Inspection runner", true, false, 9); Place(runner, Hex(0));
        var views = LoadPresentationViews(); yield return null;
        movement.ActivateMovementPhase();
        input.TryExecuteSyntheticHexClick(runner.currentHex, "Select runner", out _);
        input.TryExecuteSyntheticHexClick(Hex(2), "Move runner", out _);
        input.hoveredToken = attacker; input.SimulateKeyDataPress(KeyCode.Space);
        var inspection = views.GetComponent<RoomInspectionCard>();
        Assert.That(inspection.LockedPlayer, Is.SameAs(attacker));
        yield return Until(() => runner.currentHex == Hex(2) && !movement.isPlayerMoving && movement.movedTokens.Contains(runner), "movement continues under inspection card");
        Assert.That(inspection.LockedPlayer, Is.SameAs(attacker));
        var physicalX = new KeyPressData(KeyCode.X, false, false, false);
        Assert.That(RoomInputOwnership.ConsumeKey(physicalX, true, attacker), Is.True);
        Assert.That(movement.isMovementPhaseAttack, Is.True);
        Assert.That(movement.AttackingMovementForfeited, Is.False);
        input.TryExecuteSyntheticKeyPress("X", "CPU continuation under card", out string error);
        Assert.That(movement.isMovementPhaseDef, Is.True, error);
        Assert.That(inspection.LockedPlayer, Is.SameAs(attacker));
        input.SimulateKeyDataPress(KeyCode.Space);
        Assert.That(inspection.IsOpen, Is.False);
    }

    public IEnumerator RunMovedSlotPresentation()
    {
        Board(1); Subscribe(movement);
        match.gameData.gameSettings.homeKit = "042"; match.gameData.gameSettings.awayKit = "029";
        var runner = Token("Slot runner", true, false, 9); Place(runner, Hex(0));
        movement.ActivateMovementPhase();
        var views = LoadPresentationViews(); yield return null;
        var panel = views.GetComponentInChildren<MovementPhaseMovedTokensPanel>(true);
        var serialized = new SerializedObject(panel);
        MovementPhaseMovedTokenSlotView[] Slots(string field)
        {
            var property = serialized.FindProperty(field);
            return Enumerable.Range(0, property.arraySize).Select(i => (MovementPhaseMovedTokenSlotView)property.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
        }
        var attack = Slots("attackingMovementSlots"); var defense = Slots("defensiveMovementSlots");
        Assert.That(attack[0].State, Is.EqualTo(MovementPhaseMovedTokenSlotView.PresentationState.Active));
        Assert.That(attack[0].GetComponentInChildren<UnityEngine.UI.RawImage>().texture, Is.Not.Null, "Next slot previews the team kit before selection.");
        Assert.That(attack[0].GetComponentsInChildren<TMPro.TMP_Text>().Single(t => t.name == "Number").text, Is.Empty);
        Assert.That(attack.Skip(1).All(s => s.State == MovementPhaseMovedTokenSlotView.PresentationState.Empty), Is.True);
        input.TryExecuteSyntheticHexClick(runner.currentHex, "Slot selection", out _); yield return null;
        Assert.That(attack[0].GetComponentsInChildren<TMPro.TMP_Text>().Single(t => t.name == "Number").text, Is.EqualTo("9"));
        match.SetPauseMenuOpen(true);
        float pulse = (float)typeof(MovementPhaseMovedTokenSlotView).GetField("pulseTime", Private).GetValue(attack[0]);
        yield return null;
        Assert.That((float)typeof(MovementPhaseMovedTokenSlotView).GetField("pulseTime", Private).GetValue(attack[0]), Is.EqualTo(pulse));
        match.SetPauseMenuOpen(false);
        input.TryExecuteSyntheticHexClick(Hex(2), "Slot movement", out _);
        yield return Until(() => movement.movedTokens.Contains(runner) && !movement.isPlayerMoving, "completed slot movement");
        yield return null;
        Assert.That(attack[0].State, Is.EqualTo(MovementPhaseMovedTokenSlotView.PresentationState.Completed));
        Assert.That(attack[1].State, Is.EqualTo(MovementPhaseMovedTokenSlotView.PresentationState.Active));
        var completedTexture = attack[0].GetComponentInChildren<UnityEngine.UI.RawImage>().texture;
        LegacyKey("X"); yield return null;
        Assert.That(attack.Skip(1).All(s => s.State == MovementPhaseMovedTokenSlotView.PresentationState.Forfeited), Is.True);
        var cross = attack[1].transform.Find("Forfeit").GetComponent<TMPro.TMP_Text>();
        var border = attack[1].transform.Find("ForfeitBorder").GetComponent<TMPro.TMP_Text>();
        Assert.That(cross.text, Is.EqualTo("X")); Assert.That(border.text, Is.EqualTo("X"));
        Assert.That(border.fontSize, Is.GreaterThan(cross.fontSize));
        Assert.That(border.transform.GetSiblingIndex(), Is.LessThan(cross.transform.GetSiblingIndex()));
        Assert.That(attack[0].GetComponentInChildren<UnityEngine.UI.RawImage>().texture, Is.SameAs(completedTexture));
        Assert.That(defense[0].State, Is.EqualTo(MovementPhaseMovedTokenSlotView.PresentationState.Active));
        LegacyKey("X"); yield return null;
        Assert.That(attack.Skip(1).All(s => s.State == MovementPhaseMovedTokenSlotView.PresentationState.Forfeited), Is.True);
        Assert.That(defense.All(s => s.State == MovementPhaseMovedTokenSlotView.PresentationState.Forfeited), Is.True);
        LegacyKey("X"); yield return null;
        Assert.That(panel.GetComponent<CanvasGroup>().alpha, Is.EqualTo(0f));
        Assert.That(attack.All(s => s.State == MovementPhaseMovedTokenSlotView.PresentationState.Empty), Is.True);
        movement.ResetMovementPhase(); movement.ActivateMovementPhase(); yield return null;
        Assert.That(attack[0].State, Is.EqualTo(MovementPhaseMovedTokenSlotView.PresentationState.Active));
        Assert.That(attack.Skip(1).All(s => s.State == MovementPhaseMovedTokenSlotView.PresentationState.Empty), Is.True);
    }

    public IEnumerator RunMovementForfeitPresentation(int side, bool rejectDefense)
    {
        Board(side); Subscribe(movement);
        movement.ActivateMovementPhase();
        // Selection itself is an observable state change before any token completes.
        input.TryExecuteSyntheticHexClick(attacker.currentHex, "Presentation selection", out _);
        Assert.That(movement.selectedToken, Is.SameAs(attacker));
        Assert.That(movement.MovedTokenEntries, Is.Empty);
        LegacyKey("X");
        Assert.That(movement.isMovementPhaseDef, Is.True);
        Assert.That(movement.AttackingMovementForfeited, Is.True);
        Assert.That(movement.DefensiveMovementForfeited, Is.False);
        if (rejectDefense)
        {
            Place(defender, Hex(10 * side));
            movement.ApplyThrowInRestrictions(attacker, attacker.currentHex, 2);
            LegacyKey("X");
            Assert.That(movement.isMovementPhaseDef, Is.True);
            Assert.That(movement.DefensiveMovementForfeited, Is.False, "Rejected X cannot produce presentation marks.");
            Assert.That(movement.AttackingMovementForfeited, Is.True, "Earlier sub-phase forfeits persist.");
            movement.ClearThrowInRestrictions();
        }
        LegacyKey("X");
        Assert.That(movement.isMovementPhase2f2, Is.True);
        Assert.That(movement.AttackingMovementForfeited && movement.DefensiveMovementForfeited, Is.True);
        Assert.That(movement.MovedTokenEntries, Is.Empty, "Forfeits never fabricate completed token entries.");
        LegacyKey("X");
        Assert.That(movement.isActivated, Is.False);
        movement.ResetMovementPhase();
        Assert.That(movement.AttackingMovementForfeited || movement.DefensiveMovementForfeited, Is.False);
        Assert.That(movement.MovedTokenEntries, Is.Empty);
        yield break;
    }

    public IEnumerator RunDribblerPresentationForfeit()
    {
        yield return RunDribbleForfeitLegacy();
        Assert.That(movement.AttackingMovementForfeited || movement.DefensiveMovementForfeited, Is.False,
            "Stopping remaining pace completes the dribbler instead of forfeiting unused team slots.");
        Assert.That(movement.MovedTokenEntries.Count, Is.EqualTo(2));
        Assert.That(movement.MovedTokenEntries.Select(e => e.token).Distinct().Count(), Is.EqualTo(2));
        Assert.That(movement.MovedTokenEntries.All(e => e.section == MovementPhaseMovedTokenSection.AttMP), Is.True);
    }
}
