// Assets/Tests/EditMode/PlayerPushPull_EditModeTest.cs
#if UNITY_EDITOR
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class PlayerPushPull_EditModeTests
{
    private const float kFrame = 1f / 60f;

    private int _groundLayer;
    private int _pushableLayer;

    private PhysicsMaterial2D _origMat;

    [SetUp]
    public void SetUp()
    {
        _groundLayer = LayerMask.NameToLayer("Ground");
        if (_groundLayer < 0) _groundLayer = 0;

        _pushableLayer = LayerMask.NameToLayer("Pushable");
        if (_pushableLayer < 0) _pushableLayer = 0;

        _origMat = new PhysicsMaterial2D("OrigMat") { friction = 0.6f, bounciness = 0f };

        Physics2D.simulationMode = SimulationMode2D.Script;
    }

    [TearDown]
    public void TearDown()
    {
        Physics2D.simulationMode = SimulationMode2D.FixedUpdate;
    }

    // ---------- helpers ----------
    private static void StepFrames(int frames)
    {
        for (int i = 0; i < frames; i++)
            Physics2D.Simulate(kFrame);
    }

    private static GameObject MakeGround(Vector3 pos, Vector2 size, int layer)
    {
        var go = new GameObject("Ground") { layer = layer };
        var col = go.AddComponent<BoxCollider2D>();
        col.size = size;
        go.transform.position = pos;
        return go;
    }

    private static GameObject MakePushable(Vector3 pos, int layer, PhysicsMaterial2D mat)
    {
        var go = new GameObject("Pushable") { layer = layer };
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 1f;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1f, 1f);
        col.sharedMaterial = mat;

        go.AddComponent<PushPullObject>(); // must exist in Runtime asmdef
        go.transform.position = pos;
        return go;
    }

    private static void SetPrivate(object obj, string field, object value)
    {
        var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        var t = obj.GetType();

        var f = t.GetField(field, flags);
        if (f != null) { f.SetValue(obj, value); return; }

        var p = t.GetProperty(field, flags);
        if (p != null) { p.SetValue(obj, value); return; }

        var backing = t.GetField($"<{field}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        if (backing != null) backing.SetValue(obj, value);
    }

    private static void ToggleInteractViaReflection(PlayerPushPull ppp)
    {
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var t = typeof(PlayerPushPull);

        var currentObjField = t.GetField("currentObject", flags);
        var current = currentObjField?.GetValue(ppp);

        if (current != null)
        {
            t.GetMethod("DetachObject", flags)?.Invoke(ppp, null);
        }
        else
        {
            t.GetMethod("TryAttachToFrontObject", flags)?.Invoke(ppp, null);
        }
    }

    private GameObject MakePlayerConfigured(out PlayerPushPull ppp, int groundLayer, int pushableLayer)
    {
        var player = new GameObject("Player");

        var rb = player.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 1f;

        var col = player.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.9f, 1.6f);

        var sprite = new GameObject("Sprite");
        sprite.transform.SetParent(player.transform, false);
        sprite.AddComponent<SpriteRenderer>();

        ppp = player.AddComponent<PlayerPushPull>();

        // Important: wire LayerMasks and a generous interact range
        SetPrivate(ppp, "groundMask", (LayerMask)(1 << groundLayer));
        SetPrivate(ppp, "pushableLayer", (LayerMask)(1 << pushableLayer));
        SetPrivate(ppp, "interactRange", 1.2f);
        SetPrivate(ppp, "leashMaxDistanceX", 0.6f);
        SetPrivate(ppp, "leashGraceSeconds", 0.05f);
        SetPrivate(ppp, "noContactGraceSeconds", 0.05f);
        SetPrivate(ppp, "reduceFrictionWhileAttached", true);
        SetPrivate(ppp, "attachedFriction", 0.08f);

        return player;
    }

    // ========== TESTS ==========

    [UnityTest]
    public IEnumerator AttachAndDetach_TogglesSliderAndFriction()
    {
        MakeGround(new Vector3(0, -1f, 0), new Vector2(20, 1), _groundLayer);

        var player = MakePlayerConfigured(out var ppp, _groundLayer, _pushableLayer);
        player.transform.position = Vector3.zero;

        var pushable = MakePushable(new Vector3(1.1f, 0f, 0f), _pushableLayer, _origMat);
        var pushCol  = pushable.GetComponent<Collider2D>();

        StepFrames(2);

        // Attach
        ToggleInteractViaReflection(ppp);
        StepFrames(1);

        var slider = player.GetComponent<SliderJoint2D>();
        Assert.IsNotNull(slider, "SliderJoint2D should be created");
        Assert.AreEqual(pushable.GetComponent<Rigidbody2D>(), slider.connectedBody, "Slider connects to pushable");
        Assert.LessOrEqual(pushCol.sharedMaterial.friction, 0.09f, "Friction should be reduced while attached");

        // Detach
        ToggleInteractViaReflection(ppp);
        StepFrames(1);

        Assert.IsNull(player.GetComponent<SliderJoint2D>(), "SliderJoint2D destroyed on detach");
        Assert.AreEqual(_origMat, pushCol.sharedMaterial, "Friction restored on detach");

        Object.DestroyImmediate(player);
        Object.DestroyImmediate(pushable);
        yield return null;
    }

    [UnityTest]
    public IEnumerator Leash_TooFarX_AutoDetachAfterGrace()
    {
        MakeGround(new Vector3(0, -1f, 0), new Vector2(20, 1), _groundLayer);

        var player = MakePlayerConfigured(out var ppp, _groundLayer, _pushableLayer);
        player.transform.position = Vector3.zero;

        var pushable = MakePushable(new Vector3(1.1f, 0f, 0f), _pushableLayer, _origMat);

        StepFrames(2);

        ToggleInteractViaReflection(ppp);
        StepFrames(1);
        Assert.IsNotNull(player.GetComponent<SliderJoint2D>(), "Should be attached first");

        // exceed leash
        pushable.transform.position = new Vector3(3.5f, 0f, 0f);
        StepFrames(10);

        Assert.IsNull(player.GetComponent<SliderJoint2D>(), "Leash exceeded; should auto-detach");

        Object.DestroyImmediate(player);
        Object.DestroyImmediate(pushable);
        yield return null;
    }

    [UnityTest]
    public IEnumerator NoContact_AutoDetachAfterGrace()
    {
        MakeGround(new Vector3(0, -1f, 0), new Vector2(20, 1), _groundLayer);

        var player = MakePlayerConfigured(out var ppp, _groundLayer, _pushableLayer);
        player.transform.position = Vector3.zero;

        var pushable = MakePushable(new Vector3(1.1f, 0f, 0f), _pushableLayer, _origMat);

        StepFrames(2);

        ToggleInteractViaReflection(ppp);
        StepFrames(1);
        Assert.IsNotNull(player.GetComponent<SliderJoint2D>(), "Should be attached first");

        // break contact
        pushable.transform.position += new Vector3(0f, 3f, 0f);
        StepFrames(10);

        Assert.IsNull(player.GetComponent<SliderJoint2D>(), "Lost contact; should auto-detach");

        Object.DestroyImmediate(player);
        Object.DestroyImmediate(pushable);
        yield return null;
    }

    [UnityTest]
    public IEnumerator Attach_Fails_WhenObjectTooHighAbove()
    {
        MakeGround(new Vector3(0, -1f, 0), new Vector2(20, 1), _groundLayer);

        var player = MakePlayerConfigured(out var ppp, _groundLayer, _pushableLayer);
        player.transform.position = Vector3.zero;

        // place high so vertical slack fails
        var pushable = MakePushable(new Vector3(1.1f, 1.8f, 0f), _pushableLayer, _origMat);

        StepFrames(2);

        ToggleInteractViaReflection(ppp);
        StepFrames(2);

        Assert.IsNull(player.GetComponent<SliderJoint2D>(), "Object too high; attach should fail");

        Object.DestroyImmediate(player);
        Object.DestroyImmediate(pushable);
        yield return null;
    }
}
#endif
