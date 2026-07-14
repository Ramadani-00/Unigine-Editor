using System.Collections;
using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "d0e8c314dc6c7e150600d2c45b413d4e665c802e")]
public class ShipUI : Component
{
    private WidgetLabel speedText;
    private WidgetLabel headingText;
    private WidgetSlider throttleBar;
    private WidgetButton startButton;
    private WidgetLabel engineStatus;

    private WidgetCanvas radarCanvas;
    private List<Node> targets = new List<Node>();

    private Node ship;
    private move move;

    private bool engineStarted = false;
    private GunBaseController gunBase;
    private GunLauncher gunLauncher;
    public Node radarNode;
    private float sweepAngle = 0.0f;

    void Init()
    {
        Gui gui = Gui.GetCurrent();

        // ===== PANEL KIRI =====
        WidgetWindow hudWindow = new WidgetWindow(gui, "SHIP CONTROL");
        hudWindow.SetPosition(20, 20);
        hudWindow.SetPadding(10, 10, 10, 10);
        hudWindow.SetSpace(5, 8);
        gui.AddChild(hudWindow, Gui.ALIGN_OVERLAP);

        speedText = new WidgetLabel(gui, "Speed : 0.0 kn");
        hudWindow.AddChild(speedText, Gui.ALIGN_LEFT);

        headingText = new WidgetLabel(gui, "Heading : 0°");
        hudWindow.AddChild(headingText, Gui.ALIGN_LEFT);

        throttleBar = new WidgetSlider(gui, -5, 5, 0);
        hudWindow.AddChild(throttleBar, Gui.ALIGN_LEFT);

        engineStatus = new WidgetLabel(gui, "● ENGINE OFF");
        engineStatus.FontColor = new vec4(1.0f, 0.2f, 0.2f, 1.0f);
        hudWindow.AddChild(engineStatus, Gui.ALIGN_LEFT);

        startButton = new WidgetButton(gui, "Start Engine");
        hudWindow.AddChild(startButton, Gui.ALIGN_LEFT);
        startButton.EventClicked.Connect(OnStartButtonClicked);

        // ===== PANEL TENGAH (GUN CONTROL) =====
        WidgetWindow weaponWindow = new WidgetWindow(gui, "WEAPON CONTROL");
        weaponWindow.SetPosition(17, 225);
        weaponWindow.SetPadding(10, 10, 10, 10);
        weaponWindow.SetSpace(5, 8);
        gui.AddChild(weaponWindow, Gui.ALIGN_OVERLAP);

        WidgetButton fireButton = new WidgetButton(gui, "Fire Main Gun");
        weaponWindow.AddChild(fireButton, Gui.ALIGN_LEFT);
        fireButton.EventClicked.Connect(OnFire);
        WidgetButton leftBtn = new WidgetButton(gui, "← LEFT");
        weaponWindow.AddChild(leftBtn, Gui.ALIGN_LEFT);
        leftBtn.EventClicked.Connect(OnLeft);
        WidgetButton rightBtn = new WidgetButton(gui, "RIGHT →");
        weaponWindow.AddChild(rightBtn, Gui.ALIGN_LEFT);
        rightBtn.EventClicked.Connect(OnRight);
        WidgetButton upBtn = new WidgetButton(gui, "↑ UP");
        weaponWindow.AddChild(upBtn, Gui.ALIGN_LEFT);
        upBtn.EventClicked.Connect(OnUp);
        WidgetButton downBtn = new WidgetButton(gui, "↓ DOWN");
        weaponWindow.AddChild(downBtn, Gui.ALIGN_LEFT);
        downBtn.EventClicked.Connect(OnDown);

        // ===== PANEL KANAN (INFO KAPAL) =====
        WidgetWindow infoWindow = new WidgetWindow(gui, "SHIP INFO");
        infoWindow.SetPadding(10, 10, 10, 10);
        infoWindow.SetSpace(4, 6);

        infoWindow.SetPosition(-20, 20);
        gui.AddChild(infoWindow, Gui.ALIGN_OVERLAP | Gui.ALIGN_RIGHT | Gui.ALIGN_TOP);

        WidgetVBox infoVBox = new WidgetVBox(gui);
        infoVBox.SetSpace(4, 6);
        infoWindow.AddChild(infoVBox, Gui.ALIGN_EXPAND);

        WidgetSprite shipImage = new WidgetSprite(gui, "ui/874.png");
        shipImage.Width = 220;
        shipImage.Height = 120;
        infoVBox.AddChild(shipImage, Gui.ALIGN_TOP);

        WidgetLabel separator = new WidgetLabel(gui, "------------------------------");
        separator.FontColor = new vec4(0.5f, 0.5f, 0.5f, 1.0f);
        infoVBox.AddChild(separator, Gui.ALIGN_LEFT);

        WidgetLabel nameValue = new WidgetLabel(gui, "KRI Belati - 622");
        nameValue.FontSize = 16;
        infoVBox.AddChild(nameValue, Gui.ALIGN_LEFT);

        WidgetLabel sizeHeader = new WidgetLabel(gui, "DIMENSIONS, METRES");
        sizeHeader.FontColor = new vec4(0.6f, 0.8f, 1.0f, 1.0f);
        infoVBox.AddChild(sizeHeader, Gui.ALIGN_LEFT);

        WidgetLabel sizeValue = new WidgetLabel(gui, "62 x 9 x 4,85");
        infoVBox.AddChild(sizeValue, Gui.ALIGN_LEFT);

        WidgetLabel weaponHeader = new WidgetLabel(gui, "ARMAMENTS");
        weaponHeader.FontColor = new vec4(0.6f, 0.8f, 1.0f, 1.0f);
        infoVBox.AddChild(weaponHeader, Gui.ALIGN_LEFT);

        WidgetLabel weaponValue = new WidgetLabel(gui,
            "- Leonardo Marlin 40mm\n- meriam 20mm");
        infoVBox.AddChild(weaponValue, Gui.ALIGN_LEFT);

        //Konek KE KAPAL
        ship = World.GetNodeByName("KRIBelati");

        if (ship != null)
            move = ship.GetComponent<move>();

        Node gunNode = World.GetNodeByName("GunBase");

        if (gunNode != null)
        {
            gunBase = gunNode.GetComponent<GunBaseController>();

            Node launcherNode = gunNode.FindNode("Launcher");
            if (launcherNode != null)
            {
                gunLauncher = launcherNode.GetComponent<GunLauncher>();
            }
        }

        // //Target Radar
        // Node enemy1 = World.GetNodeByName("Enemy1");
        // Node enemy2 = World.GetNodeByName("Enemy2");

        // if (enemy1 != null) targets.Add(enemy1);
        // if (enemy2 != null) targets.Add(enemy2);
    }

    private void OnStartButtonClicked()
    {
        engineStarted = !engineStarted;

        if (move != null)
            move.EngineOn = engineStarted;

        if (engineStarted)
        {
            startButton.Text = "Stop Engine";
            engineStatus.Text = "● ENGINE ON";
            engineStatus.FontColor = new vec4(0.2f, 1.0f, 0.2f, 1.0f);
        }
        else
        {
            startButton.Text = "Start Engine";
            engineStatus.Text = "● ENGINE OFF";
            engineStatus.FontColor = new vec4(1.0f, 0.2f, 0.2f, 1.0f);

            // reset throttle
            throttleBar.Value = 0;
            if (move != null)
                move.throttleLevel = 0;
        }
    }

    // ===== WEAPON CONTROL CALLBACKS =====
    void OnFire()
    {
        if (gunLauncher != null)
            gunLauncher.Fire();
    }

    void OnLeft()
    {
        if (gunBase != null)
            gunBase.AddRotation(10.0f);
    }

    void OnRight()
    {
        if (gunBase != null)
            gunBase.AddRotation(-10.0f);
    }

    void OnUp()
    {
        if (gunLauncher != null)
            gunLauncher.AddPitch(5.0f);
    }

    void OnDown()
    {
        if (gunLauncher != null)
            gunLauncher.AddPitch(-5.0f);
    }
    void UpdateRadar()
    {
        if (radarCanvas == null || ship == null) return;

        radarCanvas.Clear();

        int size = 200;
        int center = size / 2;
        float range = 200.0f;

        // --- 1. Logika Garis Berputar (Muter-muter) ---
        sweepAngle += Game.IFps * 150.0f; // Kecepatan putar
        if (sweepAngle > 360.0f) sweepAngle -= 360.0f;

        float sweepRad = sweepAngle * MathLib.DEG2RAD;
        int sweepLine = radarCanvas.AddLine();
        radarCanvas.SetLineColor(sweepLine, new vec4(0, 1, 0, 0.5f)); // Hijau transparan
        radarCanvas.AddLinePoint(sweepLine, new vec3(center, center, 0));
        radarCanvas.AddLinePoint(sweepLine, new vec3(
            center + MathLib.Sin(sweepRad) * center,
            center + MathLib.Cos(sweepRad) * center,
            0));

        // --- 2. Ambil Arah Kapal ---
        vec3 forward = (radarNode != null) ?
            (vec3)radarNode.WorldTransform.AxisY :
            (vec3)ship.WorldTransform.AxisY;

        float heading = MathLib.Atan2(forward.x, forward.y);

        // --- 3. Gambar Target (Dibuat lebih besar) ---
        foreach (Node t in targets)
        {
            if (t == null) continue;

            vec3 offset = (vec3)(t.WorldPosition - ship.WorldPosition);
            float cos = MathLib.Cos(-heading);
            float sin = MathLib.Sin(-heading);

            float rx = offset.x * cos - offset.y * sin;
            float ry = offset.x * sin + offset.y * cos;

            int px = (int)(center + (rx / range) * center);
            int py = (int)(center + (ry / range) * center);

            if (px > 5 && px < size - 5 && py > 5 && py < size - 5)
            {
                // Gambar kotak kecil 4x4 supaya kelihatan
                int p = radarCanvas.AddPolygon();
                radarCanvas.SetPolygonColor(p, new vec4(1, 0, 0, 1));
                radarCanvas.AddPolygonPoint(p, new vec3(px - 2, py - 2, 0));
                radarCanvas.AddPolygonPoint(p, new vec3(px + 2, py - 2, 0));
                radarCanvas.AddPolygonPoint(p, new vec3(px + 2, py + 2, 0));
                radarCanvas.AddPolygonPoint(p, new vec3(px - 2, py + 2, 0));
            }
        }
    }

    public void Update()
    {
        if (move == null) return;

        float speed_knots = engineStarted ? move.CurrentSpeedMS / 0.514444f : 0.0f;
        speedText.Text = $"Speed : {speed_knots:0.0} kn";

        vec3 forward = new vec3(ship.WorldTransform.GetColumn3(1));
        float heading = MathLib.Atan2(forward.x, forward.y) * MathLib.RAD2DEG;
        if (heading < 0) heading += 360;
        headingText.Text = $"Heading : {heading:0}°";

        if (engineStarted)
        {
            move.throttleLevel = throttleBar.Value;
        }
    }
}