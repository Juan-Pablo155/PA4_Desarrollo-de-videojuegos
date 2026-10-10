using System;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using BosquePA4;

public static class PA4Builder
{
    const string ScenePath = "Assets/PA4/Escenas/Valle_PA4.unity";
    public static void Validate()
    {
        EditorSceneManager.OpenScene(ScenePath);
        var game = UnityEngine.Object.FindFirstObjectByType<PA4Game>();
        var nav = UnityEngine.Object.FindFirstObjectByType<NavMeshSurface>();
        if (!game || !game.player || game.crystals.Length != 5 || game.guards.Length < 1 || !nav.navMeshData || !game.sound.ambience.clip || !game.sound.pickup || game.effects.pool.Length < 2)
        {
            throw new Exception("Referencias PA4 incompletas");
        }

        foreach (var c in game.crystals)
        {
            var path = new NavMeshPath();
            if (!NavMesh.CalculatePath(game.player.transform.position, c.transform.position, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete)
            {
                throw new Exception("Cristal inaccesible: " + c.name);
            }

            Debug.Log("PA4_ROUTE: " + c.name + " " + c.transform.position);
        }

        var extractionPath = new NavMeshPath();
        if (!NavMesh.CalculatePath(game.player.transform.position, game.extraction.position, NavMesh.AllAreas, extractionPath) || extractionPath.status != NavMeshPathStatus.PathComplete)
        {
            throw new Exception("Mirador inaccesible");
        }

        Debug.Log("PA4_VALIDATE_OK: referencias y rutas completas");
    }

    [MenuItem("PA4/Compilar ejecutable Windows")]
    public static void Build()
    {
        Validate();
        foreach (var a in UnityEngine.Object.FindObjectsByType<NavMeshAgent>(FindObjectsSortMode.None))
        {
            a.enabled = false;
        }

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        var result = BuildPipeline.BuildPlayer(new[] { ScenePath }, "Builds/PA4_Final/ElEcoDeLosCristales.exe", BuildTarget.StandaloneWindows64, BuildOptions.None);
        if (result.summary.result != BuildResult.Succeeded)
        {
            throw new Exception("Compilacion fallida: " + result.summary.result);
        }

        Debug.Log("PA4_BUILD_OK");
    }

}
