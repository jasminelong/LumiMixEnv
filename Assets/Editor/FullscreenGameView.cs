#if UNITY_EDITOR

using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// FullscreenGameView utility
/// 
/// CN: 一个用于在主显示器上以全屏打开 Unity Game 视图的小工具。快捷键会切换窗口（Window → General → Game (Fullscreen) 或 Ctrl/Cmd+Shift+Alt+2）。
/// EN: Little utility for opening a "Game" view in fullscreen on whatever monitor Unity treats as main. Hotkey toggles the window.
/// JP: メインモニタに Unity の Game ビューを全画面で表示する小さなユーティリティ。ホットキーでウィンドウをトグルします。
/// 
/// Notes:
/// CN: 如果发生异常可通过 Alt+F4 关闭弹出窗口（前提是编辑器未处于播放模式）。
/// EN: Fullscreen popup can be closed with Alt+F4 if needed, provided the editor is not in play mode.
/// JP: 必要なら Alt+F4 でポップアップを閉じられます（エディタがプレイモードでないことが前提）。
/// </summary>
public static class FullscreenGameView
{
    // CN: UnityEditor 内部的 GameView 类型引用（反射获取）。
    // EN: Reflection Type for UnityEditor.GameView.
    // JP: UnityEditor.GameView の型をリフレクションで取得するための Type。
    static readonly Type GameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");

    // CN: GameView 的非公开属性 showToolbar（用于在全屏时隐藏工具栏）。
    // EN: Non-public property 'showToolbar' on GameView used to hide toolbar in fullscreen.
    // JP: GameView の非公開プロパティ showToolbar（全画面でツールバーを隠すために使用）。
    static readonly PropertyInfo ShowToolbarProperty = GameViewType?.GetProperty("showToolbar", BindingFlags.Instance | BindingFlags.NonPublic);

    // CN: 一个装箱的 false 值，避免重复装箱分配（仅为原则性优化）。
    // EN: Boxed false to avoid repeated boxing allocations.
    // JP: false をボックス化して繰り返しボクシングを避ける（パフォーマンス配慮）。
    static readonly object False = false;

    // CN: 当前创建的全屏 GameView 窗口实例（如果存在）。
    // EN: Current fullscreen GameView EditorWindow instance (if created).
    // JP: 作成された全画面 GameView の EditorWindow インスタンス（存在する場合）。
    static EditorWindow instance;

    // CN: 菜单项：Window/General/Game (Fullscreen) ，快捷键 Ctrl/Cmd+Shift+Alt+2（优先级 2）。
    // EN: Menu item that toggles the fullscreen Game view. Shortcut displayed in attribute.
    // JP: メニュー項目（Window/General/Game (Fullscreen)）。ショートカットは属性に示す。
    [MenuItem("Window/General/Game (Fullscreen) %#&2", priority = 2)]
    public static void Toggle()
    {
        // CN: 如果无法通过反射找到 GameView 类型则报错并返回。
        // EN: Bail out if GameView Type is not found via reflection.
        // JP: リフレクションで GameView 型が見つからない場合は終了。
        if (GameViewType == null)
        {
            Debug.LogError("GameView type not found.");
            return;
        }

        // CN: showToolbar 属性如果未找到则记录警告，但仍尝试打开窗口。
        // EN: Warn if showToolbar property not found, but continue.
        // JP: showToolbar プロパティが見つからない場合は警告を出すが続行する。
        if (ShowToolbarProperty == null)
        {
            Debug.LogWarning("GameView.showToolbar property not found.");
        }

        // CN: 如果已经打开实例则关闭；否则创建并以弹出窗口全屏展示。
        // EN: If an instance exists close it; otherwise create, configure and show fullscreen popup.
        // JP: 既にインスタンスがあれば閉じ、なければ作成して全画面ポップアップとして表示する。
        if (instance != null)
        {
            instance.Close();
            instance = null;
        }
        else
        {
            instance = (EditorWindow)ScriptableObject.CreateInstance(GameViewType);

            // CN: 尝试在创建实例后隐藏工具栏（如果属性存在）。
            // EN: Try to hide toolbar on the new instance if property exists.
            // JP: 作成したインスタンスでツールバーを隠す（プロパティがあれば）。
            ShowToolbarProperty?.SetValue(instance, False);

            // CN: 采用主显示器分辨率或当前屏幕分辨率来设置全屏矩形。
            // EN: Use Display.main or Screen resolution to determine fullscreen rect.
            // JP: Display.main または Screen の解像度を用いて全画面矩形を決定する。
            int screenWidth = Display.main.systemWidth > 0 ? Display.main.systemWidth : Screen.width;
            int screenHeight = Display.main.systemHeight > 0 ? Display.main.systemHeight : Screen.height;
            var desktopResolution = new Vector2(screenWidth, screenHeight);
            var fullscreenRect = new Rect(Vector2.zero, desktopResolution);

            // CN: 以弹出窗口方式显示并将位置设为全屏，随后获取焦点。
            // EN: Show as popup, set to fullscreen rect and focus the window.
            // JP: ポップアップで表示し、位置を全画面矩形に設定してフォーカスを与える。
            instance.ShowPopup();
            instance.position = fullscreenRect;
            instance.Focus();
        }
    }
}

#endif