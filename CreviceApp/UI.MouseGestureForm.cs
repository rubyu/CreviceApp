﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Crevice.UI
{
    using Crevice.Logging;
    using Crevice.Config;
    using Crevice.UserScript.Keys;
    using Crevice.GestureMachine;
    using WinAPI.WindowsHookEx;

    public class MouseGestureForm : Form
    {
        private bool _enableHook = false;
        protected bool EnableHook
        {
            get { return _enableHook; }
            set
            {
                if (_enableHook != value)
                {
                    if (value)
                    {
                        KeyboardHook.SetHook();
                        MouseHook.SetHook();
                        _enableHook = true;
                    }
                    else
                    {
                        KeyboardHook.Unhook();
                        MouseHook.Unhook();
                        _enableHook = false;
                    }
                }
            }
        }

        private readonly Core.Events.NullEvent NullEvent = new Core.Events.NullEvent();

        private readonly LowLevelKeyboardHook KeyboardHook;
        private readonly LowLevelMouseHook MouseHook;
        protected readonly GlobalConfig GlobalConfig;
        public readonly ReloadableGestureMachine ReloadableGestureMachine;

        public MouseGestureForm()
            : this(new GlobalConfig())
        { }

        public MouseGestureForm(GlobalConfig globalConfig)
        {
            KeyboardHook = new LowLevelKeyboardHook(KeyboardProc);
            MouseHook = new LowLevelMouseHook(MouseProc);
            GlobalConfig = globalConfig;
            ReloadableGestureMachine = new ReloadableGestureMachine(globalConfig);
        }
        
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            ReloadableGestureMachine.Dispose();
        }

        protected const int WM_DISPLAYCHANGE = 0x007E;
        protected const int WM_POWERBROADCAST = 0x0218;

        protected const int PBT_APMQUERYSUSPEND = 0x0000;
        protected const int PBT_APMQUERYSTANDBY = 0x0001;
        protected const int PBT_APMQUERYSUSPENDFAILED = 0x0002;
        protected const int PBT_APMQUERYSTANDBYFAILED = 0x0003;
        protected const int PBT_APMSUSPEND = 0x0004;
        protected const int PBT_APMSTANDBY = 0x0005;
        protected const int PBT_APMRESUMECRITICAL = 0x0006;
        protected const int PBT_APMRESUMESUSPEND = 0x0007;
        protected const int PBT_APMRESUMESTANDBY = 0x0008;
        protected const int PBT_APMBATTERYLOW = 0x0009;
        protected const int PBT_APMPOWERSTATUSCHANGE = 0x000A;
        protected const int PBT_APMOEMEVENT = 0x000B;
        protected const int PBT_APMRESUMEAUTOMATIC = 0x0012;

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_DISPLAYCHANGE:
                    Verbose.Print("WndProc: WM_DISPLAYCHANGE");
                    ReloadableGestureMachine.Instance.Reset();
                    Verbose.Print("GestureMachine was reset.");
                    break;

                case WM_POWERBROADCAST:
                    int reason = m.WParam.ToInt32();
                    switch(reason)
                    {
                        case PBT_APMQUERYSUSPEND:
                            Verbose.Print("WndProc: PBT_APMQUERYSUSPEND");
                            break;
                        case PBT_APMQUERYSTANDBY:
                            Verbose.Print("WndProc: PBT_APMQUERYSTANDBY");
                            break;
                        case PBT_APMQUERYSUSPENDFAILED:
                            Verbose.Print("WndProc: PBT_APMQUERYSUSPENDFAILED");
                            break;
                        case PBT_APMQUERYSTANDBYFAILED:
                            Verbose.Print("WndProc: PBT_APMQUERYSTANDBYFAILED");
                            break;
                        case PBT_APMSUSPEND:
                            Verbose.Print("WndProc: PBT_APMSUSPEND");
                            break;
                        case PBT_APMSTANDBY:
                            Verbose.Print("WndProc: PBT_APMSTANDBY");
                            break;
                        case PBT_APMRESUMECRITICAL:
                            Verbose.Print("WndProc: PBT_APMRESUMECRITICAL");
                            break;
                        case PBT_APMRESUMESUSPEND:
                            Verbose.Print("WndProc: PBT_APMRESUMESUSPEND");
                            break;
                        case PBT_APMRESUMESTANDBY:
                            Verbose.Print("WndProc: PBT_APMRESUMESTANDBY");
                            break;
                        case PBT_APMBATTERYLOW:
                            Verbose.Print("WndProc: PBT_APMBATTERYLOW");
                            break;
                        case PBT_APMPOWERSTATUSCHANGE:
                            Verbose.Print("WndProc: PBT_APMPOWERSTATUSCHANGE");
                            break;
                        case PBT_APMOEMEVENT:
                            Verbose.Print("WndProc: PBT_APMOEMEVENT");
                            break;
                        case PBT_APMRESUMEAUTOMATIC:
                            Verbose.Print("WndProc: PBT_APMRESUMEAUTOMATIC");
                            break;
                    }
                    break;
            }
            base.WndProc(ref m);
        }

        public WindowsHook.Result KeyboardProc(LowLevelKeyboardHook.Event evnt, LowLevelKeyboardHook.KBDLLHOOKSTRUCT data)
        {
            Debug.Print("KeyboardEvent: {0} - {1} | {2}",
                    data.vkCode,
                    Enum.GetName(typeof(LowLevelKeyboardHook.Event), evnt),
                    BitConverter.ToString(BitConverter.GetBytes((int)data.dwExtraInfo))
                    );

            if (data.FromCreviceApp)
            {
                Debug.Print("{0} was passed to the next hook because this event has the signature of CreviceApp",
                    Enum.GetName(typeof(LowLevelKeyboardHook.Event),
                    evnt));
                return WindowsHook.Result.Transfer;
            }

            var keyCode = data.vkCode;
            if (keyCode < 8 || keyCode > 255)
            {
                return WindowsHook.Result.Transfer;
            }

            var key = SupportedKeys.PhysicalKeys[(int)keyCode];

            switch (evnt)
            {
                case LowLevelKeyboardHook.Event.WM_KEYDOWN:
                    return ToHookResult(ReloadableGestureMachine.Instance.Input(key.PressEvent));

                case LowLevelKeyboardHook.Event.WM_SYSKEYDOWN:
                    return ToHookResult(ReloadableGestureMachine.Instance.Input(key.PressEvent));

                case LowLevelKeyboardHook.Event.WM_KEYUP:
                    return ToHookResult(ReloadableGestureMachine.Instance.Input(key.ReleaseEvent));

                case LowLevelKeyboardHook.Event.WM_SYSKEYUP:
                    return ToHookResult(ReloadableGestureMachine.Instance.Input(key.ReleaseEvent));
            }
            return WindowsHook.Result.Transfer;
        }

        public WindowsHook.Result MouseProc(LowLevelMouseHook.Event evnt, LowLevelMouseHook.MSLLHOOKSTRUCT data)
        {
            Debug.Print("MouseEvent: {0} | {1}",
                    Enum.GetName(typeof(LowLevelMouseHook.Event), evnt),
                    BitConverter.ToString(BitConverter.GetBytes((int)data.dwExtraInfo))
                    );

            if (data.FromCreviceApp)
            {
                Debug.Print("{0} was passed to the next hook because this event has the signature of CreviceApp",
                    Enum.GetName(typeof(LowLevelMouseHook.Event),
                    evnt));
                return WindowsHook.Result.Transfer;
            }
            else if (data.FromTablet)
            {
                Debug.Print("{0} was passed to the next hook because this event has the signature of Tablet",
                    Enum.GetName(typeof(LowLevelMouseHook.Event),
                    evnt));
                return WindowsHook.Result.Transfer;
            }

            var point = new Point(data.pt.x, data.pt.y);

            switch (evnt)
            {
                case LowLevelMouseHook.Event.WM_MOUSEMOVE:
                    return ToHookResult(ReloadableGestureMachine.Instance.Input(NullEvent, point));
                case LowLevelMouseHook.Event.WM_LBUTTONDOWN:
                    return ToHookResult(ReloadableGestureMachine.Instance.Input(SupportedKeys.PhysicalKeys.LButton.PressEvent, point));
                case LowLevelMouseHook.Event.WM_LBUTTONUP:
                    return ToHookResult(ReloadableGestureMachine.Instance.Input(SupportedKeys.PhysicalKeys.LButton.ReleaseEvent, point));
                case LowLevelMouseHook.Event.WM_RBUTTONDOWN:
                    return ToHookResult(ReloadableGestureMachine.Instance.Input(SupportedKeys.PhysicalKeys.RButton.PressEvent, point));
                case LowLevelMouseHook.Event.WM_RBUTTONUP:
                    return ToHookResult(ReloadableGestureMachine.Instance.Input(SupportedKeys.PhysicalKeys.RButton.ReleaseEvent, point));
                case LowLevelMouseHook.Event.WM_MBUTTONDOWN:
                    return ToHookResult(ReloadableGestureMachine.Instance.Input(SupportedKeys.PhysicalKeys.MButton.PressEvent, point));
                case LowLevelMouseHook.Event.WM_MBUTTONUP:
                    return ToHookResult(ReloadableGestureMachine.Instance.Input(SupportedKeys.PhysicalKeys.MButton.ReleaseEvent, point));
                case LowLevelMouseHook.Event.WM_MOUSEWHEEL:
                    if (data.mouseData.asWheelDelta.delta < 0)
                    {
                        return ToHookResult(ReloadableGestureMachine.Instance.Input(SupportedKeys.PhysicalKeys.WheelDown.FireEvent, point));
                    }
                    else
                    {
                        return ToHookResult(ReloadableGestureMachine.Instance.Input(SupportedKeys.PhysicalKeys.WheelUp.FireEvent, point));
                    }
                case LowLevelMouseHook.Event.WM_XBUTTONDOWN:
                    if (data.mouseData.asXButton.IsXButton1)
                    {
                        return ToHookResult(ReloadableGestureMachine.Instance.Input(SupportedKeys.PhysicalKeys.XButton1.PressEvent, point));
                    }
                    else
                    {
                        return ToHookResult(ReloadableGestureMachine.Instance.Input(SupportedKeys.PhysicalKeys.XButton2.PressEvent, point));
                    }
                case LowLevelMouseHook.Event.WM_XBUTTONUP:
                    if (data.mouseData.asXButton.IsXButton1)
                    {
                        return ToHookResult(ReloadableGestureMachine.Instance.Input(SupportedKeys.PhysicalKeys.XButton1.ReleaseEvent, point));
                    }
                    else
                    {
                        return ToHookResult(ReloadableGestureMachine.Instance.Input(SupportedKeys.PhysicalKeys.XButton2.ReleaseEvent, point));
                    }
                case LowLevelMouseHook.Event.WM_MOUSEHWHEEL:
                    if (data.mouseData.asWheelDelta.delta < 0)
                    {
                        return ToHookResult(ReloadableGestureMachine.Instance.Input(SupportedKeys.PhysicalKeys.WheelRight.FireEvent, point));
                    }
                    else
                    {
                        return ToHookResult(ReloadableGestureMachine.Instance.Input(SupportedKeys.PhysicalKeys.WheelLeft.FireEvent, point));
                    }
            }
            return WindowsHook.Result.Transfer;
        }

        protected WindowsHook.Result ToHookResult(bool consumed)
            => consumed ? WindowsHook.Result.Cancel : WindowsHook.Result.Transfer;
    }
}
