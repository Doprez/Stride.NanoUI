using Stride.Input;

// map to NanoUI
using UIKey = NanoUI.Common.Key;
using UIKeyModifiers = NanoUI.Common.KeyModifiers;
using UIPointerButton = NanoUI.Common.PointerButton;

namespace NanoUIDemo.CodeOnly;
internal class NanoInputMapping
{
    public static UIKeyModifiers KeyModifiers { get; private set; } = UIKeyModifiers.None;

    static void SetModifierKey(UIKeyModifiers modifier, bool down)
    {
        if (down)
        {
            if ((KeyModifiers & modifier) == 0)
                KeyModifiers |= modifier;
        }
        else
        {
            if ((KeyModifiers & modifier) != 0)
                KeyModifiers &= ~modifier;
        }
    }

    // note: modifier keys returns false (they are stored/removed in this class - not in main program)
    // isRepeat marks that we store this key & use is with when key is down
    // (normally OnKeyDown/Up is fired only once)
    // used primarily with non-char keys: Backspace, Delete (todo: navigation keys: up, down, left, right)

    // note: since NanoUI keys are 1:1 with Silk.NET.Input keys, we could just check modifiers & repeat keys & Unknown
    // and convert rest straight away. this way it's still easier(?) to understand, if user uses some other windowing/key system.
    public static bool TryMapKey(Keys key, bool down, out UIKey result, out bool isRepeat)
    {
        // default
        isRepeat = false;

        UIKey KeyToUIKey(Keys keyToConvert, Keys startKey1, UIKey startKey2)
        {
            int changeFromStart1 = (int)keyToConvert - (int)startKey1;
            return startKey2 + changeFromStart1;
        }

        if (key >= Keys.F1 && key <= Keys.F24)
        {
            result = KeyToUIKey(key, Keys.F1, UIKey.F1);
            return true;
        }
        else if (key >= Keys.NumPad0 && key <= Keys.NumPad9)
        {
            result = KeyToUIKey(key, Keys.NumPad0, UIKey.Keypad0);
            return true;
        }
        else if (key >= Keys.A && key <= Keys.Z)
        {
            result = KeyToUIKey(key, Keys.A, UIKey.A);
            return true;
        }
        else if (key >= Keys.D0 && key <= Keys.D9)
        {
            result = KeyToUIKey(key, Keys.D0, UIKey.Number0);
            return true;
        }

        switch (key)
        {
            case Keys.LeftShift:
                result = UIKey.ShiftLeft;
                SetModifierKey(UIKeyModifiers.Shift, down);
                return false;
            case Keys.RightShift:
                result = UIKey.ShiftRight;
                SetModifierKey(UIKeyModifiers.Shift, down);
                return false;
            case Keys.LeftCtrl:
                result = UIKey.ControlLeft;
                SetModifierKey(UIKeyModifiers.Control, down);
                return false;
            case Keys.RightCtrl:
                result = UIKey.ControlRight;
                SetModifierKey(UIKeyModifiers.Control, down);
                return false;
            case Keys.LeftAlt:
                result = UIKey.AltLeft;
                SetModifierKey(UIKeyModifiers.Alt, down);
                return false;
            case Keys.RightAlt:
                result = UIKey.AltRight;
                SetModifierKey(UIKeyModifiers.Alt, down);
                return false;
            //case Keys.Menu:
            //    result = UIKey.Menu;
            //    return true;
            case Keys.Up:
                result = UIKey.Up;
                return true;
            case Keys.Down:
                result = UIKey.Down;
                return true;
            case Keys.Left:
                result = UIKey.Left;
                return true;
            case Keys.Right:
                result = UIKey.Right;
                return true;
            case Keys.Enter:
                result = UIKey.Enter;
                return true;
            case Keys.Escape:
                result = UIKey.Escape;
                return true;
            case Keys.Space:
                result = UIKey.Space;
                return true;
            case Keys.Tab:
                result = UIKey.Tab;
                return true;
            case Keys.BackSpace:
                result = UIKey.Backspace;
                isRepeat = true;
                return true;
            case Keys.Insert:
                result = UIKey.Insert;
                return true;
            case Keys.Delete:
                result = UIKey.Delete;
                isRepeat = true;
                return true;
            case Keys.PageUp:
                result = UIKey.PageUp;
                return true;
            case Keys.PageDown:
                result = UIKey.PageDown;
                return true;
            case Keys.Home:
                result = UIKey.Home;
                return true;
            case Keys.End:
                result = UIKey.End;
                return true;
            case Keys.CapsLock:
                result = UIKey.CapsLock;
                return true;
            case Keys.Scroll:
                result = UIKey.ScrollLock;
                return true;
            case Keys.PrintScreen:
                result = UIKey.PrintScreen;
                return true;
            case Keys.Pause:
                result = UIKey.Pause;
                return true;
            case Keys.NumLock:
                result = UIKey.NumLock;
                return true;
            case Keys.Divide:
                result = UIKey.KeypadDivide;
                return true;
            case Keys.Multiply:
                result = UIKey.KeypadMultiply;
                return true;
            case Keys.Subtract:
                result = UIKey.KeypadSubtract;
                return true;
            case Keys.Add:
                result = UIKey.KeypadAdd;
                return true;
            case Keys.Decimal:
                result = UIKey.KeypadDecimal;
                return true;
            case Keys.NumPadEnter:
                result = UIKey.KeypadEnter;
                return true;
            //case Keys.OemPlus:
            //    result = UIKey.KeypadEqual;
            //    return true;
            //case Keys.GraveAccent:
            //    result = UIKey.GraveAccent;
            //    return true;
            case Keys.OemMinus:
                result = UIKey.Minus;
                return true;
            case Keys.OemPlus:
                result = UIKey.Equal;
                return true;
            case Keys.OemOpenBrackets:
                result = UIKey.LeftBracket;
                return true;
            case Keys.OemCloseBrackets:
                result = UIKey.RightBracket;
                return true;
            case Keys.OemSemicolon:
                result = UIKey.Semicolon;
                return true;
            //case Keys.Apostrophe:
            //    result = UIKey.Apostrophe;
            //    return true;
            case Keys.OemComma:
                result = UIKey.Comma;
                return true;
            case Keys.OemPeriod:
                result = UIKey.Period;
                return true;
            //case Keys.Slash:
            //    result = UIKey.Slash;
            //    return true;
            case Keys.OemBackslash:
                result = UIKey.BackSlash;
                return true;
            //case Keys.World1:
            //    result = UIKey.World1;
            //    return true;
            //case Keys.World2:
            //    result = UIKey.World2;
            //    return true;
            //case Keys.SuperLeft: // osx - todo :we should convert this to "standard" modifiers
            //    result = UIKey.SuperLeft;
            //    return true;
            //case Keys.SuperRight:  // osx- todo : we should convert this to "standard" modifiers
            //    result = UIKey.SuperRight;
            //    return true;
            default:
                result = UIKey.Unknown;
                return false;
        }
    }

    public static UIPointerButton MapMouseButtons(MouseButton mouseButton) => mouseButton switch
    {
        MouseButton.Left => UIPointerButton.Left,
        MouseButton.Middle => UIPointerButton.Middle,
        MouseButton.Right => UIPointerButton.Right,
        MouseButton.Extended1 => UIPointerButton.Button4,
        MouseButton.Extended2 => UIPointerButton.Button5,
        //MouseButton.Button6 => UIPointerButton.Button6,
        //MouseButton.Button7 => UIPointerButton.Button7,
        //MouseButton.Button8 => UIPointerButton.Button8,
        //MouseButton.Button9 => UIPointerButton.Button9,
        //MouseButton.Button10 => UIPointerButton.Button10,
        //MouseButton.Button11 => UIPointerButton.Button11,
        //MouseButton.Button12 => UIPointerButton.Button12,
        _ => UIPointerButton.LastButton
    };
}
