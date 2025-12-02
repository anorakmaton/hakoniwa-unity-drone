using UnityEngine;
using System.Collections;

public class PointerSetting : MonoBehaviour
{
    public bool isOpened { get { return _status != Status.Closed; } }

    public  static readonly int SettingButtonLeft = 18;
    public  static readonly int SettingButtonTop = 24;
    public  static readonly int SettingButtonWidth = 150;
    public  static readonly int SettingButtonHeight = 42;
    private static readonly int Margin = 25;
    private static readonly int MarginBox = 15;
    private static readonly int TitleHeight = 40;
    private static readonly int PageWidth = 300;
    private static readonly int PageHeight = 400;
    private static readonly int CloseButtonWidth = 20;
    private static readonly int CloseButtonHeight = 20;

    private HapticPointerBase[] _pointers;
    private PointerParameter[] _parameters;
    private Texture _settingTexture;
    private Texture _closeTexture;
    private GUIStyle _captionStyle;
    private GUIStyle _deviceNameStyle;
    private int _current = 0;
    private int _current2 = 0;
    private Status _fromStatus = 0;
    private uint [] _deviceList = null;

    enum Status
    {
        Closed,
        Opened,
        DeviceListOpened,
    }
    private Status _status = Status.Closed;

    void Awake()
    {
        //_pointers = GameObject.FindObjectsOfType(typeof(HapticPointerBase)) as HapticPointerBase[];
        _pointers = Object.FindObjectsByType<HapticPointerBase>(FindObjectsSortMode.None);
        SortedList sl = new SortedList();
        for (int i = 0; i < _pointers.Length; ++i)
            sl[_pointers[i].name] = _pointers[i];

        if (sl.Count == _pointers.Length)
        {
            HapticPointerBase[] pointers = new HapticPointerBase[_pointers.Length];
            for (int i = 0; i < _pointers.Length; ++i)
                pointers[i] = sl.GetByIndex(i) as HapticPointerBase;
            _pointers = pointers;
        }

        _parameters = new PointerParameter[_pointers.Length];
        for (int i = 0; i < _parameters.Length; ++i)
        {
            _parameters[i] = new PointerParameter(_pointers[i]);
            _parameters[i].record();
            _parameters[i].deserialize();
        }

        _settingTexture = (Texture)Resources.Load("setting", typeof(Texture2D));
        _closeTexture = (Texture)Resources.Load("close", typeof(Texture2D));

        GUIStyleState state = new GUIStyleState();
        state.textColor = Color.white;
        _captionStyle = new GUIStyle();
        _captionStyle.fontSize = 20;
        _captionStyle.normal = state;

        state = new GUIStyleState();
        state.textColor = Color.yellow;
        _deviceNameStyle = new GUIStyle();
        _deviceNameStyle.fontSize = 20;
        _deviceNameStyle.alignment = TextAnchor.MiddleLeft;
        _deviceNameStyle.normal = state;
    }

    void OnGUI()
    {
        if (_status != Status.DeviceListOpened)
            for(int i = 0; i < _pointers.Length; ++i)
                if (_pointers[i].RequestInit)
                {
                    _current2 = i;
                    _fromStatus = _status;
                    _status = Status.DeviceListOpened;
                    _deviceList = TokyoTech.Spidar.Spidar.DeviceList;
                    break;
                }

        switch (_status)
        {
            case Status.Closed:
                OnGUIClosed();
                break;

            case Status.Opened:
                OnGUIOpened();
                break;

            case Status.DeviceListOpened:
                OnGUIDeviceListOpened();
                break;
        }
    }

    void OnGUIClosed()
    {
        if (_pointers.Length == 0)
            return;

        GUIContent contents = new GUIContent();
        contents.image = _settingTexture;
        contents.text = "  SPIDAR Setting";

        GUILayout.BeginArea(new Rect(SettingButtonLeft, SettingButtonTop, SettingButtonWidth, SettingButtonHeight));
        if (GUILayout.Button(contents, GUILayout.MaxHeight(SettingButtonHeight)))
        {
            _status = Status.Opened;
        }
        GUILayout.EndArea();
    }

    void OnGUIOpened()
    {
        if (_pointers.Length == 0)
            return;

        // base
        //

        int devPageWidth = 0;
        if (_pointers.Length > 2)
            devPageWidth = Margin + PageWidth;
        else
            devPageWidth = (Margin + PageWidth) * _pointers.Length;

        int left = MarginBox;
        int top = MarginBox;
        int width = PageWidth + devPageWidth + MarginBox * 2;
        int height = PageHeight + TitleHeight + Margin + MarginBox * 2;

        GUI.Box(new Rect(left, top, width, height), "");
        GUI.Box(new Rect(left, top, width, height), "");

        if (GUI.Button(new Rect(left + width - CloseButtonWidth - MarginBox,
                            top + MarginBox, CloseButtonWidth, CloseButtonHeight), _closeTexture))
        {
            _status = Status.Closed;
        }


        // title
        //

        left = Margin;
        top = Margin;
        width = Screen.width;
        height = TitleHeight;

        GUIContent contents = new GUIContent();
        contents.image = _settingTexture;
        contents.text = "  SPIDAR Setting";

        GUILayout.BeginArea(new Rect(left, top, width, height));
        GUILayout.Label(contents, GUILayout.MaxHeight(height));
        GUILayout.EndArea();


        // Simulation settings
        //

        left = Margin;
        top = Margin + TitleHeight + MarginBox;
        width = PageWidth;
        height = PageHeight;

        GUILayout.BeginArea(new Rect(left, top, width, height));
        GUILayout.Label("Simulation", _captionStyle);
        GUILayout.Space(10);

        bool debugMode = _pointers[0].DebugMode;
        bool haptics = _pointers[0].Haptics;
        bool gravity = _pointers[0].Gravity;
        float fixedDeltaTime = HapticPointerBase.FixedDeltaTime;
        float unitySpringK = _pointers[0].UnitySpringK;
        float unityDamperB = _pointers[0].UnityDamperB;
        ShowLabeledItem("Debug Mode", ref debugMode);
        ShowLabeledItem("Haptics", ref haptics);
        ShowLabeledItem("Gravity", ref gravity);
        ShowLabeledItem("Fixed Delta Time", ref fixedDeltaTime, "f3");
        ShowLabeledItem("Spring K", ref unitySpringK, "f1");
        ShowLabeledItem("Damper B", ref unityDamperB, "f1");
        for(int i = 0; i < _pointers.Length; ++i)
        {
            _pointers[i].DebugMode = debugMode;
            _pointers[i].Haptics = haptics;
            _pointers[i].Gravity = gravity;
            _pointers[i].UnitySpringK = unitySpringK;
            _pointers[i].UnityDamperB = unityDamperB;
        }
        HapticPointerBase.FixedDeltaTime = fixedDeltaTime;

        GUILayout.Space(10);
        if (GUILayout.Button("Revert to the default"))
            for (int i = 0; i < _parameters.Length; ++i)
                _parameters[i].recall(true);

        GUILayout.EndArea();


        // Device settings
        //

        if (_pointers.Length > 2)
        {
            left += width + Margin;
            top = Margin + TitleHeight + MarginBox;
            width = PageWidth;
            height = PageHeight;

            GUILayout.BeginArea(new Rect(left, top, width, height));
            GUILayout.BeginHorizontal();
            GUILayout.Label("Device :", _captionStyle);
            GUILayout.Label(_pointers[_current].name, _deviceNameStyle);
            if (GUILayout.Button(">", GUILayout.MaxWidth(20)))
            {
                _current++;
                if (_current >= _pointers.Length)
                    _current = 0;
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            OnGUIOpenedDeviceOf(_current);

            GUILayout.EndArea();
        }
        else
        {
            for (int i = 0; i < _pointers.Length; ++i)
            {
                left += width + Margin;
                top = Margin + TitleHeight + MarginBox;
                width = PageWidth;
                height = PageHeight;

                GUILayout.BeginArea(new Rect(left, top, width, height));
                GUILayout.BeginHorizontal();
                GUILayout.Label("Device :", _captionStyle);
                GUILayout.Label(_pointers[i].name, _deviceNameStyle);
                GUILayout.EndHorizontal();
                GUILayout.Space(10);

                OnGUIOpenedDeviceOf(i);

                GUILayout.EndArea();
            }
        }

        //
        for (int i = 0; i < _parameters.Length; ++i)
            _parameters[i].check();
    }

    void OnGUIDeviceListOpened()
    {
        if (_pointers.Length == 0)
            return;

        if (_fromStatus == Status.Opened)
        {
            GUI.enabled = false;
            OnGUIOpened();
            GUI.enabled = true;
        }

        // base
        //

        int left = MarginBox;
        int top = MarginBox;
        int width = PageWidth + MarginBox * 2;
        int height = PageHeight + TitleHeight + Margin + MarginBox * 2;
        if (_fromStatus == Status.Opened)
        {
            left += PageWidth;
            top += 20;
        }

        GUI.Box(new Rect(left, top, width, height), "");
        GUI.Box(new Rect(left, top, width, height), "");

        if (GUI.Button(new Rect(left + width - CloseButtonWidth - MarginBox,
                            top + MarginBox, CloseButtonWidth, CloseButtonHeight), _closeTexture))
        {
            _status = _fromStatus;
            _pointers[_current2].Initialize();
        }


        // title
        //

        left = Margin;
        top = Margin;
        width = Screen.width;
        height = TitleHeight;
        if (_fromStatus == Status.Opened)
        {
            left += PageWidth;
            top += 20;
        }

        GUIContent contents = new GUIContent();
        contents.image = _settingTexture;
        contents.text = "  SPIDAR Setting";

        GUILayout.BeginArea(new Rect(left, top, width, height));
        GUILayout.Label(contents, GUILayout.MaxHeight(height));
        GUILayout.EndArea();


        // Device selection
        //

        left = Margin;
        top = Margin + TitleHeight + MarginBox;
        width = PageWidth;
        height = PageHeight;
        if (_fromStatus == Status.Opened)
        {
            left += PageWidth;
            top += 20;
        }

        GUILayout.BeginArea(new Rect(left, top, width, height));
        GUILayout.Label("Device :   " + _pointers[_current2].name, _captionStyle);
        GUILayout.Space(10);

        GUILayout.Label("Device list");

        bool selected;
        for (int i = 0; i < _deviceList.Length; ++i)
        {
            selected = _pointers[_current2].SerialNumber == _deviceList[i];

            selected = GUILayout.Toggle(selected, _deviceList[i].ToString());

            if (selected)
                _pointers[_current2].SerialNumber = _deviceList[i];
        }
        selected = _pointers[_current2].SerialNumber == 0;
        selected = GUILayout.Toggle(selected, "Not assigned");

        if (selected)
            _pointers[_current2].SerialNumber = 0;


        GUILayout.EndArea();
    }

    void OnGUIOpenedDeviceOf(int deviceNo)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("Serial number:");
        GUILayout.Label(_pointers[deviceNo].SerialNumber.ToString());
        if (GUILayout.Button("..", GUILayout.MaxWidth(20)))
        {
            _current2 = deviceNo;
            _fromStatus = _status;
            _status = Status.DeviceListOpened;
            _deviceList = TokyoTech.Spidar.Spidar.DeviceList;
        }
        GUILayout.EndHorizontal();

        //ShowLabeledItem("Device model", ref _pointers[deviceNo].DeviceModel);
        ShowLabeledItem("Position scale", ref _pointers[deviceNo].PositionScale, "f3");
        ShowLabeledItem("Rotation scale", ref _pointers[deviceNo].RotationScale, "f3", (_pointers[deviceNo] as HapticPointer) != null);
        ShowLabeledItem("Spring K", ref _pointers[deviceNo].DeviceSpringK, "f1");
        ShowLabeledItem("Damper B", ref _pointers[deviceNo].DeviceDamperB, "f1");
        ShowLabeledItem("Cascade control", ref _pointers[deviceNo].CascadeControl);
        ShowLabeledItem("Cascade gain", ref _pointers[deviceNo].CascadeGain, "f1");
        ShowLabeledItem("Toggle hold", ref _pointers[deviceNo].ToggleHold, 
            (_pointers[deviceNo] as HapticPointer) != null || (_pointers[deviceNo] as HakoDroneSpidarInputManager) != null);
        ShowLabeledItem("Toggle clutch", ref _pointers[deviceNo].ToggleClutch);
        ShowLabeledItem("Hold channel", ref _pointers[deviceNo].HoldChannel,
            (_pointers[deviceNo] as HapticPointer) != null || (_pointers[deviceNo] as HakoDroneSpidarInputManager) != null);
        ShowLabeledItem("Clutch channel", ref _pointers[deviceNo].ClutchChannel);
        ShowLabeledItem("Calibration channel", ref _pointers[deviceNo].CalibrationChannel);
        
        // HakoDroneSpidarInputManagerの場合のみArm channelを表示
        var hakoDroneManager = _pointers[deviceNo] as HakoDroneSpidarInputManager;
        if (hakoDroneManager != null)
        {
            ShowLabeledItem("Arm channel", ref hakoDroneManager.ArmChannel, true);
        }

        GUILayout.Label("Drone Pointer専用設定");
        if (hakoDroneManager != null)
        {
            ShowLabeledItem("Basic Force Feedback", ref hakoDroneManager.basicForceFeedback, true);
        }
        GUILayout.Space(10);
        if (GUILayout.Button("Revert to the default"))
        {
            _parameters[deviceNo].recall(false);
            _pointers[deviceNo].Initialize();
        }
    }

    void ShowLabeledItem(string label, ref bool value, bool enabled = true)
    {
        GUI.enabled = enabled;
        value = GUILayout.Toggle(value, "  " + label);
        GUI.enabled = true;
    }

    void ShowLabeledItem(string label, ref int value, bool enabled = true)
    {
        GUI.enabled = enabled;
        GUILayout.BeginHorizontal();
        GUILayout.Label(label + ":");
        try
        {
            value = int.Parse(GUILayout.TextField(value.ToString(), 10, GUILayout.MinWidth(150), GUILayout.MaxWidth(150)));
        }
        catch (System.FormatException)
        {
            value = 0;
        }
        GUILayout.EndHorizontal();
        GUI.enabled = true;
    }

    void ShowLabeledItem(string label, ref uint value, bool enabled = true)
    {
        GUI.enabled = enabled;
        GUILayout.BeginHorizontal();
        GUILayout.Label(label + ":");
        try
        {
            value = uint.Parse(GUILayout.TextField(value.ToString(), 10, GUILayout.MinWidth(150), GUILayout.MaxWidth(150)));
        }
        catch (System.FormatException)
        {
            value = 0;
        }
        GUILayout.EndHorizontal();
        GUI.enabled = true;
    }

    void ShowLabeledItem(string label, ref float value, string format, bool enabled = true)
    {
        GUI.enabled = enabled;
        GUILayout.BeginHorizontal();
        GUILayout.Label(label + ":");
        try
        {
            value = float.Parse(GUILayout.TextField(value.ToString(format), 10, GUILayout.MinWidth(150), GUILayout.MaxWidth(150)));
        }
        catch (System.FormatException)
        {
            value = 0.0f;
        }
        GUILayout.EndHorizontal();
        GUI.enabled = true;
    }

    void ShowLabeledItem(string label, ref string value, bool enabled = true)
    {
        GUI.enabled = enabled;
        GUILayout.BeginHorizontal();
        GUILayout.Label(label + ":");
        value = GUILayout.TextField(value, GUILayout.MaxWidth(200));
        GUILayout.EndHorizontal();
        GUI.enabled = true;
    }

}
