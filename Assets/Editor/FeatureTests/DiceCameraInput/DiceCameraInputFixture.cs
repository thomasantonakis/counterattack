using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed partial class GoalkeeperBoxEntryFixture
{
    public IEnumerator RunDiceCameraInput(int side)
    {
        Board(side); AttachAuthoredPanels();
        EnableForAuthoredPresentation(movement); EnableForAuthoredPresentation(loose);
        Subscribe(match.finalThirdManager);
        match.BroadcastAnyOtherScenario();
        LegacyKey("M"); LegacyKey("X");
        yield return Until(() => movement.isMovementPhaseDef, "defender turn");
        input.TryExecuteSyntheticHexClick(awayGK.currentHex, "Dice camera test", out _);
        input.TryExecuteSyntheticHexClick(Hex(12 * side), "Dice camera test", out _);
        yield return Until(() => movement.isWaitingForTackleDecision && !movement.isPlayerMoving, "tackle choice");
        LegacyKey("T");
        yield return Until(() => movement.isWaitingForTackleRoll && duelPanel.IsAwaitingRoll, "real pending duel roll");

        var cameraObject = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(FrameworkAuthoredCameraAsset.PrefabPath), root.transform);
        var camera = cameraObject.GetComponent<Camera>();
        var controller = cameraObject.AddComponent<CameraController>();
        controller.gameInputManager = input; input.cameraController = controller;
        yield return Until(() => controller.IsReady, "camera ready");
        controller.SetCameraToPreset(1);
        Assert.That(Camera.main, Is.SameAs(camera));
        Assert.That(GameInputManager.IsPresentationModalActive, Is.True);
        Assert.That((bool)typeof(CameraController).GetProperty("IsCameraInputBlocked", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).GetValue(null), Is.False);
        input.isDragging = false; // Dice input intentionally never classifies a gameplay drag.

        Vector2 center = new Vector2(camera.pixelWidth, camera.pixelHeight) * .5f;
        void Drag(Vector2 p, bool down, bool held, bool up, bool ui = false)
            => Invoke(controller, "HandlePointerDrag", p, down, held, up, ui);
        var before = controller.transform.position;
        Drag(center, true, true, false);
        Drag(center + Vector2.right * 2, false, true, false);
        Assert.That(controller.transform.position, Is.EqualTo(before), "A click is not a camera drag.");
        Drag(center, false, false, true);

        Drag(center, true, true, false, true);
        Drag(center + Vector2.right * 80, false, true, false);
        Assert.That(controller.transform.position, Is.EqualTo(before), "A gesture starting on the panel never pans, even after leaving it.");
        Drag(center, false, false, true);

        Drag(center, true, true, false);
        Drag(center + Vector2.right * 80, false, true, false);
        Assert.That(Vector3.Distance(controller.transform.position, before), Is.GreaterThan(.01f), "Pitch drag pans independently of blocked gameplay clicks.");
        Assert.That(input.isDragging, Is.False);
        Drag(center, false, false, true);
        before = controller.transform.position;
        Drag(center + Vector2.right * 100, false, true, false);
        Assert.That(controller.transform.position, Is.EqualTo(before), "Release ends the gesture.");

        var panelPosition = duelPanel.Panel.anchoredPosition;
        var handle = duelPanel.GetComponentInChildren<DuelPanelDragHandle>(true);
        Assert.That(handle, Is.Not.Null);
        var pointer = new PointerEventData(null) { button = PointerEventData.InputButton.Right, position = center };
        handle.OnBeginDrag(pointer); pointer.position += Vector2.right * 80; handle.OnDrag(pointer); handle.OnEndDrag(pointer);
        Assert.That(duelPanel.Panel.anchoredPosition, Is.EqualTo(panelPosition), "Right drag belongs to the camera.");
        var rotation = controller.transform.rotation;
        Invoke(controller, "HandlePointerRotation", new Vector2(.5f, .1f), true, true, false, .016f);
        Assert.That(Quaternion.Angle(rotation, controller.transform.rotation), Is.GreaterThan(.1f));
        Invoke(controller, "HandlePointerRotation", Vector2.zero, false, false, true, .016f);
        Assert.That(duelPanel.Panel.anchoredPosition, Is.EqualTo(panelPosition));

        pointer.button = PointerEventData.InputButton.Left; pointer.position = center;
        handle.OnBeginDrag(pointer); pointer.position += new Vector2(50, 40); handle.OnDrag(pointer); handle.OnEndDrag(pointer);
        Assert.That(duelPanel.Panel.anchoredPosition, Is.Not.EqualTo(panelPosition), "Left drag still moves the panel.");
        Assert.That(movement.isWaitingForTackleRoll && duelPanel.IsAwaitingRoll, Is.True, "Camera and panel gestures never roll dice.");

        before = controller.transform.position;
        controller.ZoomBy(2);
        Assert.That(controller.transform.position, Is.Not.EqualTo(before));
        var preset = new KeyPressData(KeyCode.Alpha2, false, false, false);
        Assert.That((bool)Invoke(input, "DispatchSyntheticKeyPress", preset, "Dice camera test"), Is.True);
        Assert.That(preset.consumedBy, Is.EqualTo(nameof(CameraController)));
        Assert.That(duelPanel.IsAwaitingRoll, Is.True);

        match.SetPauseMenuOpen(true);
        try
        {
            before = controller.transform.position; rotation = controller.transform.rotation;
            Drag(center, true, true, false); Drag(center + Vector2.right * 80, false, true, false);
            Invoke(controller, "HandlePointerRotation", Vector2.one, true, true, false, .016f);
            Assert.That(controller.transform.position, Is.EqualTo(before));
            Assert.That(controller.transform.rotation, Is.EqualTo(rotation));
        }
        finally { match.SetPauseMenuOpen(false); }

        // Rig the first roll through the real key dispatch; R then reaches the
        // attacker roll handler rather than being swallowed by camera navigation.
        input.SimulateKeyDataPress(KeyCode.Alpha5, shift: true, ctrl: true);
        var defenderDie = duelPanel.DribblerOnLeft ? duelPanel.rightPlayer.die : duelPanel.leftPlayer.die;
        Assert.That(defenderDie.DisplayedRoll, Is.EqualTo(5));
        Assert.That(defenderDie.IsAnimating, Is.False);
        yield return Until(() => duelPanel.IsAwaitingRoll, "attacker roll prompt");
        UnityEngine.Random.InitState(2468);
        var roll = new KeyPressData(KeyCode.R, false, false, false);
        Assert.That((bool)Invoke(input, "DispatchSyntheticKeyPress", roll, "Dice camera test"), Is.True);
        Assert.That(roll.consumedBy, Is.EqualTo(nameof(MovementPhaseManager)));
        Assert.That(movement.isWaitingForTackleRoll, Is.False);
        yield return Until(() => duelPanel.IsComplete, "R resolves into the visible duel result");
        Assert.That(duelPanel.IsComplete, Is.True, "R resolves the duel and enters its result presentation.");
        var attackerDie = duelPanel.DribblerOnLeft ? duelPanel.leftPlayer.die : duelPanel.rightPlayer.die;
        Assert.That(attackerDie.IsAnimating, Is.False);
        Assert.That(attackerDie.DisplayedRoll, Is.EqualTo(2));
    }
}
