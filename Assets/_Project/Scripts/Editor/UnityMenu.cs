using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;

public static class UnityMenu
{
    private const string TitleScenePath = "Assets/_Project/Scenes/Title.unity";
    private const string GameScenePath = "Assets/_Project/Scenes/Game.unity";
    private const string GameToolbarPath = "Team963/Game Controls Before Play";

    [MainToolbarElement(
        GameToolbarPath,
        defaultDockPosition = MainToolbarDockPosition.Middle,
        defaultDockIndex = 0)]
    private static IEnumerable<MainToolbarElement> CreateGameToolbar()
    {
        MainToolbarContent sceneContent = new MainToolbarContent(
            "씬 이동",
            "타이틀 또는 게임 씬으로 이동합니다.");

        Texture2D playIcon = EditorGUIUtility.IconContent("PlayButton").image as Texture2D;
        MainToolbarContent startGameContent = new MainToolbarContent(
            "방구석 몬스터 시작",
            playIcon,
            "타이틀 씬부터 게임을 시작합니다.");

        yield return new MainToolbarDropdown(sceneContent, ShowSceneDropdown);
        yield return new MainToolbarButton(startGameContent, StartGame);
    }

    private static void ShowSceneDropdown(Rect dropdownRect)
    {
        GenericMenu menu = new GenericMenu();
        menu.AddItem(new GUIContent("타이틀"), false, () => OpenScene(TitleScenePath));
        menu.AddItem(new GUIContent("게임"), false, () => OpenScene(GameScenePath));
        menu.DropDown(dropdownRect);
    }

    private static void StartGame()
    {
        if (!OpenScene(TitleScenePath))
            return;

        EditorApplication.isPlaying = true;
    }

    private static bool OpenScene(string scenePath)
    {
        if (EditorApplication.isPlaying)
            return false;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return false;

        EditorSceneManager.OpenScene(scenePath);
        return true;
    }
}
