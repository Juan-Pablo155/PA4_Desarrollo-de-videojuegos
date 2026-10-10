using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using BosquePA4;

public static class PA4Builder
{
    const string Root = "Assets/PA4";
    const string ScenePath = Root + "/Escenas/Valle_PA4.unity";
    static Terrain terrain;
    static Vector3 spawn;
    static Material jade, gold, dark, red;
    static Material Mat(string name, Color color, bool glow = false)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetColor("_BaseColor", color);
        m.SetFloat("_Smoothness", .25f);
        if (glow)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", color * 1.5f);
        }

        AssetDatabase.CreateAsset(m, Root + "/Materiales/" + name + ".mat");
        return m;
    }

    static GameObject Shape(string name, PrimitiveType type, Transform parent, Vector3 pos, Vector3 scale, Material mat)
    {
        var g = GameObject.CreatePrimitive(type);
        g.name = name;
        g.transform.SetParent(parent);
        g.transform.localPosition = pos;
        g.transform.localScale = scale;
        g.GetComponent<Renderer>().sharedMaterial = mat;
        UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
        return g;
    }

    static Vector3 Ground(Vector3 p)
    {
        p.y = terrain.SampleHeight(p) + terrain.transform.position.y;
        return p;
    }

    static Vector3 Reachable(Vector3 desired)
    {
        var path = new NavMeshPath();
        for (int ring = 0; ring < 14; ring++)
        {
            for (int i = 0; i < 12; i++)
            {
                float angle = i * Mathf.PI / 6;
                Vector3 p = Ground(desired + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * ring * 1.5f);
                if (NavMesh.SamplePosition(p, out var hit, 2, NavMesh.AllAreas) && NavMesh.CalculatePath(spawn, hit.position, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete)
                {
                    return hit.position;
                }
            }
        }

        throw new Exception("No hay ruta navegable al destino " + desired);
    }

    static ParticleSystem Effect(Transform parent, Material mat)
    {
        var g = new GameObject("Rafaga reutilizable");
        g.transform.SetParent(parent);
        var p = g.AddComponent<ParticleSystem>();
        p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = p.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = .3f;
        main.startLifetime = 1.1f;
        main.startSpeed = 3;
        main.startSize = .12f;
        main.startColor = new Color(.15f, 1, .75f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var em = p.emission;
        em.rateOverTime = 0;
        em.SetBursts(new[] { new ParticleSystem.Burst(0, 36) });
        var sh = p.shape;
        sh.shapeType = ParticleSystemShapeType.Sphere;
        sh.radius = .35f;
        p.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat;
        return p;
    }

    [MenuItem("PA4/Preparar juego sobre el terreno original")]
    public static void Prepare()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        if (File.Exists(ScenePath))
        {
            EditorSceneManager.OpenScene(ScenePath);
            return;
        }

        foreach (var d in new[]
        {
            "Escenas",
            "Materiales",
            "Prefabs",
            "Navegacion"
        }

        )
        {
            Directory.CreateDirectory(Root + "/" + d);
        }

        AssetDatabase.Refresh();
        var scene = EditorSceneManager.OpenScene("Assets/PA3/Escenas/Terreno_PA3.unity");
        var player = UnityEngine.Object.FindFirstObjectByType<FirstPersonController>();
        terrain = UnityEngine.Object.FindFirstObjectByType<Terrain>();
        if (!player || !terrain)
        {
            throw new Exception("Faltan el terreno o el personaje original");
        }

        foreach (var help in UnityEngine.Object.FindObjectsByType<PA3Ayuda>(FindObjectsSortMode.None))
        {
            help.enabled = false;
        }

        foreach (var mineral in UnityEngine.Object.FindObjectsByType<PA3Mineral>(FindObjectsSortMode.None))
        {
            mineral.gameObject.SetActive(false);
        }

        var top = new GameObject("05 PA4 - Mision del valle");
        var game = top.AddComponent<PA4Game>();
        game.player = player;
        player.walkSpeed = 3.8f;
        player.sprintSpeed = 6.3f;
        player.sprintDuration = 6;
        player.sprintCooldown = 2;
        player.enableHeadBob = false;
        player.crosshair = false;
        player.useSprintBar = false;
        player.playerCamera.farClipPlane = 450;
        player.playerCamera.nearClipPlane = .08f;
        var rb = player.GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        player.transform.position = Ground(player.transform.position) + Vector3.up * 1.2f;
        spawn = Ground(player.transform.position);
        var nav = new GameObject("06 Navegacion horneada");
        var surface = nav.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.Volume;
        surface.center = spawn;
        surface.size = new Vector3(180, 140, 180);
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.overrideVoxelSize = true;
        surface.voxelSize = .2f;
        player.gameObject.SetActive(false);
        Physics.SyncTransforms();
        surface.BuildNavMesh();
        if (!surface.navMeshData)
        {
            throw new Exception("No se genero NavMesh");
        }

        AssetDatabase.CreateAsset(surface.navMeshData, Root + "/Navegacion/ValleNavMesh.asset");
        if (!NavMesh.SamplePosition(spawn, out var start, 5, NavMesh.AllAreas))
        {
            throw new Exception("Inicio fuera del NavMesh");
        }

        spawn = start.position;
        player.transform.position = spawn + Vector3.up * 1.2f;
        player.gameObject.SetActive(true);
        jade = Mat("Cristal cian", new Color(.06f, .8f, .65f), true);
        gold = Mat("Mirador dorado", new Color(1, .64f, .12f), true);
        dark = Mat("Centinela", new Color(.1f, .14f, .18f));
        red = Mat("Ojo de alerta", new Color(1, .12f, .08f), true);
        var pm = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        pm.SetColor("_BaseColor", new Color(.2f, 1, .8f));
        AssetDatabase.CreateAsset(pm, Root + "/Materiales/Particulas.mat");
        var fx = new GameObject("07 Pool de particulas");
        game.effects = fx.AddComponent<PA4Effects>();
        game.effects.pool = new[]
        {
            Effect(fx.transform, pm),
            Effect(fx.transform, pm),
            Effect(fx.transform, pm)
        };
        var crystalsRoot = new GameObject("08 Cristales de la mision");
        Vector3[] offsets =
        {
            new Vector3(-15, 0, -23),
            new Vector3(-42, 0, 14),
            new Vector3(15, 0, 48),
            new Vector3(48, 0, 5),
            new Vector3(30, 0, -40)
        };
        game.crystals = new PA4Crystal[offsets.Length];
        for (int i = 0; i < offsets.Length; i++)
        {
            Vector3 p = Reachable(spawn + offsets[i]);
            var g = new GameObject("Cristal " + (i + 1));
            g.transform.SetParent(crystalsRoot.transform);
            g.transform.position = p + Vector3.up;
            var crystal = g.AddComponent<PA4Crystal>();
            crystal.model = Shape("Nucleo giratorio", PrimitiveType.Cube, g.transform, Vector3.zero, new Vector3(.7f, 1.1f, .7f), jade).transform;
            crystal.model.localRotation = Quaternion.Euler(0, 0, 45);
            Shape("Baliza visible", PrimitiveType.Cylinder, g.transform, Vector3.up * 2, new Vector3(.1f, 2, .1f), jade);
            var trigger = g.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 1.2f;
            PrefabUtility.SaveAsPrefabAssetAndConnect(g, Root + "/Prefabs/Cristal" + (i + 1) + ".prefab", InteractionMode.AutomatedAction);
            game.crystals[i] = crystal;
        }

        var platform = GameObject.Find("Plataforma del mirador");
        var extraction = new GameObject("09 Regreso al mirador");
        extraction.transform.position = platform.transform.position + Vector3.up * 1.25f;
        game.extraction = extraction.transform;
        Shape("Baliza de regreso", PrimitiveType.Cylinder, extraction.transform, new Vector3(1.8f, 1.2f, 0), new Vector3(.16f, 3, .16f), gold);
        Shape("Zona de entrega", PrimitiveType.Cylinder, extraction.transform, new Vector3(0, -1, 0), new Vector3(3, .035f, 3), gold);
        var er = extraction.AddComponent<SphereCollider>();
        er.isTrigger = true;
        er.radius = 2.7f;
        var audioRoot = new GameObject("10 Audio");
        game.sound = audioRoot.AddComponent<PA4Audio>();
        var a = game.sound;
        a.ambience = audioRoot.AddComponent<AudioSource>();
        a.ambience.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Audio/ambiente.wav");
        a.ambience.loop = true;
        a.ambience.playOnAwake = true;
        a.ambience.volume = .15f;
        a.fx = audioRoot.AddComponent<AudioSource>();
        a.fx.playOnAwake = false;
        a.fx.volume = .55f;
        a.pickup = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Audio/cristal.wav");
        a.hurt = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Audio/golpe.wav");
        a.win = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Audio/victoria.wav");
        a.lose = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Audio/derrota.wav");
        a.start = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Audio/inicio.wav");
        var enemies = new GameObject("11 Centinelas con IA");
        game.guards = new PA4Guard[2];
        for (int i = 0; i < 2; i++)
        {
            var g = new GameObject("Centinela " + (i + 1));
            g.transform.SetParent(enemies.transform);
            g.transform.position = Reachable(game.crystals[i * 2 + 1].transform.position + new Vector3(5, 0, 4));
            Shape("Cuerpo", PrimitiveType.Capsule, g.transform, Vector3.up * 1.1f, new Vector3(.85f, .7f, .85f), dark);
            var eye = Shape("Ojo", PrimitiveType.Sphere, g.transform, new Vector3(0, 1.6f, .38f), Vector3.one * .42f, red);
            var agent = g.AddComponent<NavMeshAgent>();
            agent.radius = .45f;
            agent.height = 2;
            agent.speed = 2;
            agent.acceleration = 12;
            agent.angularSpeed = 240;
            agent.stoppingDistance = .7f;
            var guard = g.AddComponent<PA4Guard>();
            guard.game = game;
            guard.eye = eye.transform;
            guard.waypoints = new[]
            {
                g.transform.position,
                Reachable(g.transform.position + new Vector3(12, 0, 0)),
                Reachable(g.transform.position + new Vector3(0, 0, -12))
            };
            game.guards[i] = guard;
        }

        var hud = new GameObject("12 Interfaz y menus");
        hud.AddComponent<PA4Hud>().game = game;
        terrain.detailObjectDistance = 45;
        terrain.treeDistance = 220;
        terrain.treeBillboardDistance = 90;
        terrain.detailObjectDensity = .25f;
        QualitySettings.shadowDistance = 65;
        QualitySettings.vSyncCount = 1;
        Application.targetFrameRate = 60;
        PlayerSettings.companyName = "ProyectoAcademico";
        PlayerSettings.productName = "El eco de los cristales";
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.runInBackground = true;
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath, true)
        };
        AssetDatabase.SaveAssets();
        Debug.Log("PA4_PREPARED: 5 cristales, 2 centinelas, NavMesh, UI, audio, pool y extraccion");
        Validate();
    }

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

    public static void PrepareAndBuild()
    {
        Prepare();
        Build();
    }
}
