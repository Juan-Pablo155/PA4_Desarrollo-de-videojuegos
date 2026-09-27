using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;
using System.Collections.Generic;

public static class ContinuarOriginalPA3
{
    const string Raiz="Assets/PA3_Continuacion";
    const string Escena=Raiz+"/Escenas/Terreno_PA3.unity";
    static Terrain terreno;
    static Transform nuevos;
    static Dictionary<Material,Material> materiales=new Dictionary<Material,Material>();
    static float Altura(float x,float z) => terreno.SampleHeight(new Vector3(x,0,z))+terreno.transform.position.y;
    static Material CrearMaterial(string nombre,Color color)
    {
        var m=new Material(Shader.Find("Universal Render Pipeline/Lit")); m.color=color;
        AssetDatabase.CreateAsset(m,Raiz+"/Materiales/"+nombre+".mat");return m;
    }
    static GameObject Caja(string nombre,Vector3 pos,Vector3 escala,Material mat)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=nombre;g.transform.SetParent(nuevos);g.transform.position=pos;g.transform.localScale=escala;g.GetComponent<Renderer>().sharedMaterial=mat;return g;
    }
    static void Compatibilidad(GameObject root)
    {
        foreach(var r in root.GetComponentsInChildren<Renderer>(true))
        {
            var mats=r.sharedMaterials;
            for(int i=0;i<mats.Length;i++)
            {
                var old=mats[i];if(!old || old.shader.name.StartsWith("Universal Render Pipeline/"))continue;
                if(!materiales.TryGetValue(old,out var nuevo))
                {
                    nuevo=new Material(Shader.Find("Universal Render Pipeline/Lit"));nuevo.name=old.name+"_PA3";
                    if(old.HasProperty("_Color"))nuevo.SetColor("_BaseColor",old.GetColor("_Color"));
                    if(old.HasProperty("_MainTex"))nuevo.SetTexture("_BaseMap",old.GetTexture("_MainTex"));
                    nuevo.SetFloat("_Smoothness",.15f);
                    AssetDatabase.CreateAsset(nuevo,AssetDatabase.GenerateUniqueAssetPath(Raiz+"/Materiales/"+nuevo.name+".mat"));materiales.Add(old,nuevo);
                }
                mats[i]=nuevo;
            }
            r.sharedMaterials=mats;
        }
    }
    static GameObject Decorar(string ruta,string nombre,float x,float z,float escala=1)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(ruta);if(!prefab)throw new System.Exception("No existe el recurso original: "+ruta);
        var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab);g.name=nombre;g.transform.SetParent(nuevos);g.transform.position=new Vector3(x,Altura(x,z),z);g.transform.localScale*=escala;Compatibilidad(g);return g;
    }
    [MenuItem("PA3 Original/Completar terreno recuperado")]
    public static void Completar()
    {
        if(EditorApplication.isPlaying)throw new System.Exception("Salir del modo Play antes de completar");
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        if(File.Exists(Escena)){EditorSceneManager.OpenScene(Escena);Debug.Log("PA3 original: escena existente abierta, sin regenerar");return;}
        foreach(var d in new[]{"Escenas","Materiales","Prefabs"})Directory.CreateDirectory(Raiz+"/"+d);
        AssetDatabase.Refresh();
        var scene=EditorSceneManager.OpenScene("Assets/_Recovery/0 (3).unity");
        // Guardar una continuacion dentro del mismo proyecto conserva intacta la recuperacion.
        EditorSceneManager.SaveScene(scene,Escena);
        terreno=Object.FindFirstObjectByType<Terrain>();
        var player=Object.FindFirstObjectByType<FirstPersonController>();
        if(!terreno || !player)throw new System.Exception("La recuperacion debe contener terreno y controlador");
        var originales=new GameObject("01 Avance original - terreno y rocas");
        foreach(var root in scene.GetRootGameObjects())
        {
            if(root==originales || root==player.gameObject || root.GetComponent<Light>() || root.name=="Global Volume")continue;
            root.transform.SetParent(originales.transform,true);Compatibilidad(root);
        }
        // Solo sustituir el material ausente/incompatible; los datos y texturas del terreno se conservan.
        if(!terreno.materialTemplate || !terreno.materialTemplate.shader.name.StartsWith("Universal Render Pipeline/"))
        {
            var tm=new Material(Shader.Find("Universal Render Pipeline/Terrain/Lit"));AssetDatabase.CreateAsset(tm,Raiz+"/Materiales/Terreno_URP.mat");terreno.materialTemplate=tm;
        }
        player.gameObject.name="02 FirstPersonController original";player.gameObject.AddComponent<PA3Ayuda>();
        var pos=player.transform.position;pos.y=Altura(pos.x,pos.z)+1.25f;player.transform.position=pos;
        player.transform.rotation=Quaternion.Euler(0,180,0);player.mouseSensitivity=1.4f;
        Compatibilidad(player.gameObject);
        nuevos=new GameObject("03 Ampliacion - mirador y sendero").transform;
        var madera=CrearMaterial("Madera mirador",new Color(.34f,.19f,.09f));var piedra=CrearMaterial("Piedra tallada",new Color(.44f,.47f,.4f));var borde=CrearMaterial("Bordes claros",new Color(.68f,.58f,.34f));
        // Un mirador con rampa continua, calculado sobre las alturas del terreno existente.
        float x=pos.x+10,z=pos.z-5;
        float h=Altura(x,z);
        for(int i=-3;i<=3;i++)for(int j=-3;j<=3;j++)h=Mathf.Max(h,Altura(x+i,z+j));
        h+=1.1f;
        Caja("Plataforma del mirador",new Vector3(x,h-.15f,z),new Vector3(6,.3f,6),madera);
        for(int i=-1;i<=1;i+=2)for(int j=-1;j<=1;j+=2)
        {
            float xx=x+i*2.7f,zz=z+j*2.7f,baseY=Altura(xx,zz);
            Caja("Pilar de piedra",new Vector3(xx,(h+baseY)/2,zz),new Vector3(.4f,h-baseY,.4f),piedra);
            Caja("Poste del mirador",new Vector3(xx,h+.6f,zz),new Vector3(.15f,1.2f,.15f),madera);
        }
        Caja("Baranda norte",new Vector3(x,h+1,z-2.7f),new Vector3(5.5f,.15f,.15f),borde);
        Caja("Baranda este",new Vector3(x+2.7f,h+1,z),new Vector3(.15f,.15f,5.5f),borde);
        Caja("Baranda sur",new Vector3(x,h+1,z+2.7f),new Vector3(5.5f,.15f,.15f),borde);
        float startX=x-12,startY=Altura(x-12,z)+.06f,endX=x-3;
        var inicio=new Vector3(startX,startY,z);var fin=new Vector3(endX,h,z);var delta=fin-inicio;
        var rampa=Caja("Rampa de acceso",(inicio+fin)/2-Vector3.up*.08f,new Vector3(delta.magnitude,.16f,2.5f),madera);
        rampa.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);
        string recursos="Assets/Polytope Studio/Lowpoly_Environments/Prefabs/";
        for(int i=0;i<6;i++)
        {
            float xx=pos.x-10-i%2*4,zz=pos.z+6-i*5;
            Decorar(recursos+"Trees/PT_Pine_Tree_03_green.prefab","Pino del recurso original "+(i+1),xx,zz,.65f);
        }
        for(int i=0;i<4;i++)Decorar(recursos+"Shrubs/PT_Generic_Shrub_01_green.prefab","Arbusto "+(i+1),pos.x+3+i*2,pos.z+5,.8f);
        Decorar(recursos+"Trees/PT_Pine_Tree_03_logs.prefab","Troncos del recurso original",x+5,z-3,.8f);
        // La roca interactiva es exactamente la instancia que ya figuraba en la recuperacion.
        var roca=GameObject.Find("PT_Ore_Rock_01_split");if(!roca)throw new System.Exception("Roca original ausente");
        var zona=new GameObject("04 Interaccion de la roca original");zona.transform.position=roca.transform.position;
        var box=zona.AddComponent<SphereCollider>();box.isTrigger=true;box.radius=3.5f;
        var rb=zona.AddComponent<Rigidbody>();rb.isKinematic=true;rb.useGravity=false;
        var ps=zona.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main=ps.main;main.playOnAwake=false;main.startColor=new Color(.2f,1,.8f);main.startSize=.12f;main.startSpeed=1.8f;main.startLifetime=2;main.simulationSpace=ParticleSystemSimulationSpace.World;
        var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=18;shape.radius=.7f;shape.rotation=new Vector3(-90,0,0);
        var em=ps.emission;em.rateOverTime=30;
        var pm=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));pm.SetColor("_BaseColor",new Color(.3f,1,.7f));AssetDatabase.CreateAsset(pm,Raiz+"/Materiales/Energia.mat");ps.GetComponent<ParticleSystemRenderer>().sharedMaterial=pm;
        var lg=new GameObject("Luz mineral");lg.transform.SetParent(zona.transform);lg.transform.localPosition=Vector3.up*2;var light=lg.AddComponent<Light>();light.type=LightType.Point;light.color=Color.cyan;light.range=10;light.intensity=4;light.enabled=false;
        var interact=zona.AddComponent<PA3Mineral>();interact.efecto=ps;interact.luz=light;interact.jugador=player.transform;
        PrefabUtility.SaveAsPrefabAssetAndConnect(zona,Raiz+"/Prefabs/InteraccionMineral.prefab",InteractionMode.AutomatedAction);
        var sol=Object.FindFirstObjectByType<Light>();if(sol){sol.shadows=LightShadows.Soft;}
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        var lista=new List<EditorBuildSettingsScene>{new EditorBuildSettingsScene(Escena,true)};
        foreach(var s in EditorBuildSettings.scenes)if(s.path!=Escena)lista.Add(new EditorBuildSettingsScene(s.path,false));
        EditorBuildSettings.scenes=lista.ToArray();
        Directory.CreateDirectory("PA3_Evidencias");
        File.WriteAllText("PA3_Evidencias/continuacion.txt","Base: Assets/_Recovery/0 (3).unity\nConservado: New Terrain 1, FirstPersonController, rocas mineral y menhir; prefabs Polytope.\nAñadido: mirador, rampa, vegetacion, Trigger mineral, particulas y ayuda.\nJugador: "+player.transform.position+"\nMirador: "+new Vector3(x,h,z)+"\nRampa: "+inicio+" -> "+fin);
        Debug.Log("PA3 ORIGINAL: continuacion guardada en "+Escena);
    }
    [MenuItem("PA3 Original/Capturar vista actual")]
    public static void Capturar()
    {
        if(SceneManager.GetActiveScene().path!=Escena)EditorSceneManager.OpenScene(Escena);
        var player=Object.FindFirstObjectByType<FirstPersonController>();if(!player)return;
        Directory.CreateDirectory("PA3_Evidencias");var cam=player.playerCamera;var p=cam.transform.position;var q=cam.transform.rotation;
        try{
            cam.transform.position=player.transform.position+new Vector3(-15,13,15);cam.transform.LookAt(player.transform.position+new Vector3(4,0,-7));
            var rt=new RenderTexture(1280,720,24);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
            var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();File.WriteAllBytes("PA3_Evidencias/terreno.png",tex.EncodeToPNG());
            cam.targetTexture=null;RenderTexture.active=null;Object.DestroyImmediate(rt);Object.DestroyImmediate(tex);
        }finally{cam.transform.position=p;cam.transform.rotation=q;}
        Debug.Log("PA3 ORIGINAL: captura guardada");
    }
    [MenuItem("PA3 Original/Ajustar materiales del terreno")]
    public static void Ajustar()
    {
        if(EditorApplication.isPlaying) return;
        if(SceneManager.GetActiveScene().path!=Escena)EditorSceneManager.OpenScene(Escena);
        terreno=Object.FindFirstObjectByType<Terrain>();
        string copia=Raiz+"/TerrenoConservado.asset";
        if(!AssetDatabase.LoadAssetAtPath<TerrainData>(copia))AssetDatabase.CreateAsset(Object.Instantiate(terreno.terrainData),copia);
        terreno.terrainData=AssetDatabase.LoadAssetAtPath<TerrainData>(copia);terreno.GetComponent<TerrainCollider>().terrainData=terreno.terrainData;
        var tipos=terreno.terrainData.treePrototypes;
        for(int i=0;i<tipos.Length;i++)
        {
            if(!tipos[i].prefab)continue;
            var g=(GameObject)PrefabUtility.InstantiatePrefab(tipos[i].prefab);Compatibilidad(g);
            tipos[i].prefab=PrefabUtility.SaveAsPrefabAsset(g,Raiz+"/Prefabs/ArbolTerreno_"+i+".prefab");Object.DestroyImmediate(g);
        }
        terreno.terrainData.treePrototypes=tipos;terreno.terrainData.RefreshPrototypes();
        foreach(var guid in AssetDatabase.FindAssets("t:Material",new[]{Raiz+"/Materiales"}))
        {
            var m=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));string n=m.name.ToLowerInvariant();
            if(n.Contains("leaves"))m.SetColor("_BaseColor",n.Contains("pine")?new Color(.13f,.32f,.2f):new Color(.26f,.43f,.17f));
            else if(n.Contains("trunk"))m.SetColor("_BaseColor",new Color(.28f,.17f,.09f));
            else if(n.Contains("rocks"))m.SetColor("_BaseColor",new Color(.36f,.4f,.43f));
            if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",.12f);
            EditorUtility.SetDirty(m);
        }
        terreno.detailObjectDistance=55;terreno.detailObjectDensity=.35f;
        EditorUtility.SetDirty(terreno.terrainData);EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        Debug.Log("PA3 ORIGINAL: materiales ajustados; alturas y arboles conservados en copia de TerrainData");
    }
}
