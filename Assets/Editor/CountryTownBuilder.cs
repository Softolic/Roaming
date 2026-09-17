using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.ProBuilder;

/// <summary>Builds the shared village exterior and the playable second chapter.</summary>
public static class CountryTownBuilder
{
    const string Dir = "Assets/Environment/CountryTown";
    const string Chapter1 = "Assets/Scenes/Capitulo 1.unity";
    const string Chapter2 = "Assets/Scenes/Capitulo 2.unity";
    static Material plaster, blue, ochre, rose, roof, roofEdge, wood, trim, glass, stone, dirt, grass, leaves, iron, glow;
    static Mesh roofMesh;
    static Vector3 Origin => new Vector3(4.12f - .08913043f * 460f + Mathf.Sin(460f * .038f) * 2.1f, 1.2f, 415f);

    static Material Mat(string name, Color color)
    {
        string path = Dir + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
        m.color = color;
        m.SetFloat("_Smoothness", .12f);
        EditorUtility.SetDirty(m);
        return m;
    }
    static Transform Group(Transform parent, string name, Vector3 position)
    {
        var g = new GameObject(name);
        if (parent != null) g.transform.SetParent(parent, false);
        g.transform.localPosition = position;
        return g.transform;
    }
    static GameObject Box(Transform parent, string name, Vector3 p, Vector3 size, Material mat, bool solid = true)
    {
        var pb = ShapeGenerator.GenerateCube(PivotLocation.Center, size);
        var go = pb.gameObject; go.name = name; go.transform.SetParent(parent, false); go.transform.localPosition = p;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        var col = go.GetComponent<BoxCollider>();
        if (solid) { if (col == null) col = go.AddComponent<BoxCollider>(); col.size = size; }
        else if (col != null) Object.DestroyImmediate(col);
        return go;
    }
    static void Roof(Transform parent, Vector3 p, Vector3 size)
    {
        var g = Group(parent, "Telhado de duas aguas", p).gameObject;
        g.AddComponent<MeshFilter>().sharedMesh = roofMesh;
        g.AddComponent<MeshRenderer>().sharedMaterial = roof;
        g.transform.localScale = size;
        // Visible ceramic ridge and eaves.
        Box(parent, "Cumeeira", p + Vector3.up * size.y, new Vector3(.18f,.15f,size.z+.15f), roofEdge, false);
        for (int side=-1; side<=1; side+=2)
            Box(parent, "Beiral", p+Vector3.right*side*size.x*.5f, new Vector3(.16f,.18f,size.z), trim, false);
    }
    static void Label(Transform parent, string value, Vector3 p, float width, float size)
    {
        var g = Group(parent, value, p);
        var text = g.gameObject.AddComponent<TMPro.TextMeshPro>();
        text.text = value; text.fontSize = size; text.alignment = TMPro.TextAlignmentOptions.Center;
        text.color = new Color(.96f,.89f,.7f); text.rectTransform.sizeDelta = new Vector2(width, 1.4f);
        text.font = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
    }
    static void House(Transform town, string name, float x, float z, float angle, Material wall, string shop = "")
    {
        var h = Group(town, name, new Vector3(x,0,z)); h.localRotation=Quaternion.Euler(0,angle,0);
        Box(h,"Alicerce",new Vector3(0,.15f,0),new Vector3(8.8f,.3f,7.6f),stone);
        Box(h,"Paredes",new Vector3(0,2.2f,0),new Vector3(8.2f,4.1f,7),wall);
        Box(h,"Rodape branco",new Vector3(0,.55f,-3.52f),new Vector3(8.25f,.38f,.1f),trim,false);
        Roof(h,new Vector3(0,4.3f,0),new Vector3(9.3f,2.1f,8.2f));
        Box(h,"Batente da porta",new Vector3(0,1.43f,-3.58f),new Vector3(1.75f,2.65f,.18f),trim,false);
        Box(h,"Porta de madeira",new Vector3(0,1.35f,-3.7f),new Vector3(1.4f,2.45f,.12f),wood,false);
        Box(h,"Macaneta",new Vector3(.48f,1.3f,-3.8f),new Vector3(.09f,.09f,.08f),ochre,false);
        for(int side=-1;side<=1;side+=2)
        {
            float wx=side*2.65f;
            Box(h,"Moldura da janela",new Vector3(wx,2.4f,-3.6f),new Vector3(1.6f,1.75f,.2f),trim,false);
            Box(h,"Vidro azul",new Vector3(wx,2.4f,-3.72f),new Vector3(1.29f,1.45f,.08f),glass,false);
            Box(h,"Travessa janela",new Vector3(wx,2.4f,-3.79f),new Vector3(1.3f,.08f,.06f),trim,false);
            Box(h,"Travessa janela",new Vector3(wx,2.4f,-3.8f),new Vector3(.08f,1.45f,.06f),trim,false);
            for(int shutter=-1;shutter<=1;shutter+=2)
                Box(h,"Veneziana",new Vector3(wx+shutter*.98f,2.4f,-3.6f),new Vector3(.46f,1.65f,.13f),wood,false);
            Box(h,"Floreira",new Vector3(wx,1.45f,-3.98f),new Vector3(1.8f,.32f,.46f),roofEdge,false);
            for(int j=0;j<4;j++) Box(h,"Flores",new Vector3(wx-.6f+j*.4f,1.7f,-3.98f),new Vector3(.18f,.2f,.19f),j%2==0?rose:ochre,false);
        }
        Box(h,"Chamine",new Vector3(2.4f,5.2f,1.8f),new Vector3(.85f,2f,.9f),roofEdge,false);
        Box(h,"Topo da chamine",new Vector3(2.4f,6.2f,1.8f),new Vector3(1.1f,.22f,1.15f),trim,false);
        if(!string.IsNullOrEmpty(shop))
        {
            Box(h,"Placa do comercio",new Vector3(0,3.65f,-3.75f),new Vector3(5.9f,.85f,.22f),wood,false);
            Label(h,shop,new Vector3(0,3.65f,-3.88f),5.7f,3.3f);
            var awning=Box(h,"Toldo",new Vector3(0,3f,-4.55f),new Vector3(6.4f,.14f,1.8f),wall,false);
            awning.transform.localRotation=Quaternion.Euler(-10,0,0);
            for(int side=-1;side<=1;side+=2) Box(h,"Apoio do toldo",new Vector3(side*3f,1.45f,-5.2f),new Vector3(.12f,2.9f,.12f),wood);
        }
    }
    static void Tree(Transform town,float x,float z,float scale=1)
    {
        var t=Group(town,"Arvore da praca",new Vector3(x,0,z)); t.localScale=Vector3.one*scale;
        Box(t,"Tronco",new Vector3(0,1.6f,0),new Vector3(.5f,3.2f,.5f),wood);
        for(int i=0;i<3;i++)
        {
            var pb=ShapeGenerator.GenerateIcosahedron(PivotLocation.Center,2.1f,1);
            var g=pb.gameObject;g.name="Copa";g.transform.SetParent(t,false);
            g.transform.localPosition=new Vector3((i-1)*.8f,3.6f+(i==1?.8f:0),i%2*.35f);
            g.GetComponent<Renderer>().sharedMaterial=leaves;
        }
    }
    static void Bench(Transform town,float x,float z,float angle)
    {
        var b=Group(town,"Banco de madeira",new Vector3(x,0,z));b.localRotation=Quaternion.Euler(0,angle,0);
        for(int i=0;i<3;i++) Box(b,"Tabua do assento",new Vector3(0,.65f,-.3f+i*.3f),new Vector3(2.7f,.12f,.24f),wood);
        for(int i=0;i<2;i++) Box(b,"Encosto",new Vector3(0,1.05f+i*.28f,.4f),new Vector3(2.7f,.18f,.12f),wood);
        for(int side=-1;side<=1;side+=2) Box(b,"Pe do banco",new Vector3(side, .36f,0),new Vector3(.16f,.72f,.8f),iron);
    }
    static void Fence(Transform town, float x, float z, float length, float angle)
    {
        var f=Group(town,"Cerca de madeira",new Vector3(x,0,z));f.localRotation=Quaternion.Euler(0,angle,0);
        for(float p=-length*.5f;p<=length*.5f;p+=2)
            Box(f,"Mourao",new Vector3(p,.7f,0),new Vector3(.18f,1.4f,.18f),wood);
        for(int i=0;i<2;i++) Box(f,"Travessa",new Vector3(0,.48f+i*.5f,0),new Vector3(length,.14f,.12f),wood);
    }
    static void SetupAssets()
    {
        if(!AssetDatabase.IsValidFolder(Dir)) AssetDatabase.CreateFolder("Assets/Environment","CountryTown");
        plaster=Mat("Reboco creme",new Color(.81f,.76f,.58f)); blue=Mat("Reboco azul",new Color(.27f,.53f,.61f));
        ochre=Mat("Reboco ocre",new Color(.78f,.51f,.2f));rose=Mat("Reboco rosa",new Color(.68f,.36f,.3f));
        roof=Mat("Telhas terracota",new Color(.49f,.19f,.105f));roofEdge=Mat("Ceramica escura",new Color(.32f,.12f,.075f));
        wood=Mat("Madeira",new Color(.24f,.17f,.105f));trim=Mat("Cal branca",new Color(.92f,.87f,.71f));
        glass=Mat("Vidro escuro",new Color(.105f,.23f,.25f));stone=Mat("Pedra da praca",new Color(.49f,.46f,.36f));
        dirt=Mat("Rua de terra",new Color(.51f,.38f,.22f));grass=Mat("Pasto",new Color(.31f,.38f,.17f));
        leaves=Mat("Folhas",new Color(.24f,.37f,.14f));iron=Mat("Ferro",new Color(.14f,.16f,.14f));
        glow=Mat("Luz de lampiao",new Color(1f,.72f,.29f));glow.EnableKeyword("_EMISSION");glow.SetColor("_EmissionColor",new Color(1f,.48f,.12f)*1.5f);
        roofMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Dir+"/Telhado.asset");
        if(roofMesh==null)
        {
            roofMesh=new Mesh(); roofMesh.name="Telhado de duas aguas";
            roofMesh.vertices=new[]{new Vector3(-.5f,0,-.5f),new Vector3(.5f,0,-.5f),new Vector3(0,1,-.5f),new Vector3(-.5f,0,.5f),new Vector3(.5f,0,.5f),new Vector3(0,1,.5f)};
            roofMesh.triangles=new[]{0,2,1,3,4,5,0,3,5,0,5,2,1,2,5,1,5,4,0,1,4,0,4,3};
            roofMesh.RecalculateNormals();roofMesh.RecalculateBounds();AssetDatabase.CreateAsset(roofMesh,Dir+"/Telhado.asset");
        }
    }
    static GameObject BuildTown()
    {
        var town=Group(null,"Cidade do Interior - Vila Serena",Origin);
        // Main street and low pavements leave a continuous, walkable approach.
        Box(town,"Terreno da vila",new Vector3(0,-.45f,65),new Vector3(140,.9f,130),grass);
        Box(town,"Rua principal",new Vector3(0,.025f,53),new Vector3(10,.05f,106),dirt);
        Box(town,"Rua da praca",new Vector3(0,.03f,45),new Vector3(69,.06f,9),dirt);
        for(int side=-1;side<=1;side+=2)
            Box(town,"Calcada",new Vector3(side*6.3f,.055f,49),new Vector3(2.6f,.11f,90),stone);
        // A gentle ramp connects the lowered forest trail to the village.
        var ramp=Box(town,"Ligacao com a trilha",new Vector3(0,-.39f,-5.5f),new Vector3(6,.18f,11.2f),dirt);
        ramp.transform.localRotation=Quaternion.Euler(-Mathf.Atan2(.68f,11f)*Mathf.Rad2Deg,0,0);
        House(town,"Casa azul",-11,8,-90,blue);
        House(town,"Casa amarela",11,8,90,ochre);
        House(town,"Mercearia",-13,31,-90,plaster,"MERCEARIA");
        House(town,"Padaria",13,31,90,rose,"PADARIA");
        House(town,"Casa rosa",-13,63,-90,rose);
        House(town,"Casa creme",13,68,90,plaster);
        House(town,"Casa do jardim",-28,69,-90,ochre);
        House(town,"Casa ao fundo",28,83,90,blue);
        House(town,"Casa da esquina",-27,25,0,plaster);
        // Village square on the right side of the crossing.
        Box(town,"Praca",new Vector3(22,.075f,48),new Vector3(22,.15f,22),stone);
        var well=Group(town,"Poco da praca",new Vector3(22,0,48));
        for(int i=0;i<12;i++)
        {
            float a=i*Mathf.PI*2/12;
            var b=Box(well,"Pedra do poco",new Vector3(Mathf.Sin(a)*1.35f,.62f,Mathf.Cos(a)*1.35f),new Vector3(.73f,1.05f,.35f),trim);
            b.transform.localRotation=Quaternion.Euler(0,a*Mathf.Rad2Deg,0);
        }
        Box(well,"Agua",new Vector3(0,.35f,0),new Vector3(2.2f,.05f,2.2f),glass,false);
        for(int side=-1;side<=1;side+=2)Box(well,"Suporte",new Vector3(side*1.9f,1.7f,0),new Vector3(.2f,3.4f,.2f),wood);
        Roof(well,new Vector3(0,3.3f,0),new Vector3(4.8f,1f,3.9f));
        Bench(town,17,40,180);Bench(town,27,40,180);Bench(town,17,56,0);Bench(town,27,56,0);
        Tree(town,13,41,.9f);Tree(town,31,41);Tree(town,31,56,.9f);
        // Chapel closes the long street with an identifiable rural landmark.
        var chapel=Group(town,"Capela de Vila Serena",new Vector3(0,0,96));
        Box(chapel,"Corpo",new Vector3(0,3.7f,0),new Vector3(11,7.4f,12),plaster);
        Roof(chapel,new Vector3(0,7.4f,0),new Vector3(12,3,13));
        Box(chapel,"Torre",new Vector3(0,6,-5.5f),new Vector3(3.8f,12,3.8f),trim);
        Roof(chapel,new Vector3(0,12,-5.5f),new Vector3(4.7f,2.3f,4.7f));
        Box(chapel,"Porta",new Vector3(0,1.9f,-7.46f),new Vector3(2.4f,3.8f,.12f),wood,false);
        Box(chapel,"Abertura do sino",new Vector3(0,10.1f,-7.46f),new Vector3(2,2,.12f),glass,false);
        Box(chapel,"Sino",new Vector3(0,9.8f,-7.6f),new Vector3(.75f,.8f,.2f),ochre,false);
        Box(chapel,"Cruz",new Vector3(0,15.1f,-5.5f),new Vector3(.18f,1.9f,.18f),wood,false);
        Box(chapel,"Braco da cruz",new Vector3(0,15.45f,-5.5f),new Vector3(1.1f,.18f,.18f),wood,false);
        Box(town,"Largo da capela",new Vector3(0,.06f,83),new Vector3(19,.12f,11),stone);
        for(int side=-1;side<=1;side+=2)
        {
            Fence(town,side*23,10,16,90);
            Fence(town,side*40,62,100,90);
            for(int i=0;i<5;i++)
            {
                var lamp=Group(town,"Lampiao da rua",new Vector3(side*6.3f,0,8+i*18));
                Box(lamp,"Poste",new Vector3(0,2.3f,0),new Vector3(.16f,4.6f,.16f),iron);
                Box(lamp,"Lanterna",new Vector3(0,4.65f,0),new Vector3(.45f,.65f,.45f),glow,false);
                Box(lamp,"Chapeu",new Vector3(0,5.02f,0),new Vector3(.67f,.14f,.67f),iron,false);
            }
            for(int i=0;i<6;i++)Tree(town,side*(32+i%2*4),8+i*19,.85f+i%3*.16f);
        }
        var sign=Group(town,"Placa de boas vindas",new Vector3(5,0,1));
        Box(sign,"Poste",new Vector3(0,1.4f,0),new Vector3(.2f,2.8f,.2f),wood);
        Box(sign,"Tabua",new Vector3(0,2.9f,0),new Vector3(5,1.5f,.2f),wood);
        Label(sign,"VILA SERENA",new Vector3(0,3.06f,-.13f),4.8f,3.5f);
        Label(sign,"BEM-VINDO",new Vector3(0,2.56f,-.13f),4.5f,2f);
        // Physical borders have fences/vegetation in front; the entrance remains open.
        for(int side=-1;side<=1;side+=2)
        {
            var wall=Box(town,"Limite lateral",new Vector3(side*44,5,65),new Vector3(1,10,130),grass);
            wall.GetComponent<Renderer>().enabled=false;
        }
        Fence(town,0,118,86,0);
        var back=Box(town,"Limite dos fundos",new Vector3(0,5,119),new Vector3(90,10,1),grass);
        back.GetComponent<Renderer>().enabled=false;
        return town.gameObject;
    }
    static GameObject Root(Scene scene,string name) => scene.GetRootGameObjects().First(g=>g.name==name);
    static void TextField(Object target,string name,string value)
    {
        var so=new SerializedObject(target);so.FindProperty(name).stringValue=value;so.ApplyModifiedPropertiesWithoutUndo();
    }
    static void RefField(Object target,string name,Object value)
    {
        var so=new SerializedObject(target);so.FindProperty(name).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();
    }
    [MenuItem("Roaming/Criar cidade e Capitulo 2")]
    public static void Build()
    {
        if(Application.isPlaying)throw new System.InvalidOperationException("Pare o jogo antes de construir.");
        if(AssetDatabase.LoadAssetAtPath<SceneAsset>(Chapter2)!=null)
            throw new System.InvalidOperationException("Capitulo 2 ja existe; edite a cena preservando as alteracoes.");
        var scene=SceneManager.GetActiveScene();
        if(scene.path!=Chapter1)throw new System.InvalidOperationException("Abra Capitulo 1 antes de construir.");
        SetupAssets();
        var town=BuildTown();
        var trigger=Group(null,"Entrada da cidade - Capitulo 2",new Vector3(Origin.x,3,412)).gameObject;
        var box=trigger.AddComponent<BoxCollider>();box.isTrigger=true;box.size=new Vector3(20,8,3);
        var transition=trigger.AddComponent<ForestChapterTransition>();TextField(transition,"destinationScene","Capitulo 2");
        var reveal=Root(scene,"CM vcam Player").AddComponent<CountryTownReveal>();
        RefField(reveal,"player",Root(scene,"player").transform);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        // A scene copy retains all camera, player, animation and UI references.
        EditorSceneManager.SaveScene(scene,Chapter2,true);
        var city=EditorSceneManager.OpenScene(Chapter2,OpenSceneMode.Single);
        foreach(var go in city.GetRootGameObjects())
            if(go.name=="Floresta - Capitulo 1" || go.name=="Hitboxes Refeitas" || go.name=="Cachorro da Ponte (Temporario)" || go.name=="Entrada da cidade - Capitulo 2")
                Object.DestroyImmediate(go);
        var player=Root(city,"player");
        var scent=player.GetComponent<TobyScentGuide>();if(scent!=null)Object.DestroyImmediate(scent);
        var ball=Root(city,"Bolinha Vermelha");
        var chase=ball.GetComponent<ForestBallChase>();if(chase!=null)Object.DestroyImmediate(chase);
        ball.transform.position=Origin+new Vector3(0,.2f,7);
        var pickup=player.GetComponent<TobyBallPickup>();pickup.enabled=true;
        var arrival=new GameObject("Chegada - Capitulo 2").AddComponent<CountryTownArrival>();
        RefField(arrival,"pickup",pickup);RefField(arrival,"ball",ball.GetComponent<Rigidbody>());
        var title=Root(city,"Chapter Intro").AddComponent<SceneChapterTitle>();
        TextField(title,"chapterName","CAPITULO 2");TextField(title,"subtitle","CIDADE DO INTERIOR");
        var mission=Root(city,"Mission HUD - Capitulo 1");mission.name="Mission HUD - Capitulo 2";
        TextField(mission.GetComponent<MissionController>(),"initialMission","EXPLORE A CIDADE");
        Object.DestroyImmediate(Root(city,"CM vcam Player").GetComponent<CountryTownReveal>());
        var cityCamera=Root(city,"CM vcam Player").GetComponent<Unity.Cinemachine.CinemachineCamera>();
        var lens=cityCamera.Lens;lens.OrthographicSize=12.5f;cityCamera.Lens=lens;
        var townRoot=Root(city,"Cidade do Interior - Vila Serena");
        var ground=townRoot.transform.Find("Terreno da vila");
        ground.localPosition=new Vector3(0,-.45f,45);ground.localScale=new Vector3(1,1,170f/130f);
        Object.DestroyImmediate(townRoot.transform.Find("Ligacao com a trilha").gameObject);
        Box(townRoot.transform,"Estrada de chegada",new Vector3(0,.025f,-15),new Vector3(10,.05f,30),dirt);
        var grounding=player.GetComponent<ForestSpawnGrounding>();
        grounding.Configure(townRoot.GetComponentsInChildren<Collider>());
        player.transform.SetPositionAndRotation(Origin+new Vector3(0,.7f,4),Quaternion.identity);
        var body=player.GetComponent<Rigidbody>();body.position=player.transform.position;body.rotation=player.transform.rotation;
        grounding.PlaceAboveGround();
        var follow=Root(city,"Camera pivot").GetComponent<SeguirCamera>();follow.usarLimites=false;
        follow.transform.position=player.transform.position+follow.deslocamento;
        var main=follow.GetComponentInChildren<Camera>();if(main!=null)main.farClipPlane=500;
        var sun=Root(city,"Directional Light").GetComponent<Light>();sun.color=new Color(1,.87f,.68f);sun.intensity=1.35f;
        sun.transform.rotation=Quaternion.Euler(48,-35,0);
        RenderSettings.fog=true;RenderSettings.fogDensity=.0035f;RenderSettings.fogColor=new Color(.62f,.67f,.62f);
        RenderSettings.ambientLight=new Color(.68f,.70f,.62f);
        var south=Box(townRoot.transform,"Limite da entrada",new Vector3(0,5,-9.5f),new Vector3(90,10,1),grass);south.GetComponent<Renderer>().enabled=false;
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(city);EditorSceneManager.SaveScene(city);
        var scenes=EditorBuildSettings.scenes.ToList();
        if(!scenes.Any(s=>s.path==Chapter2))scenes.Add(new EditorBuildSettingsScene(Chapter2,true));
        EditorBuildSettings.scenes=scenes.ToArray();
        Debug.Log("[Cidade] Vila Serena criada no final do Capitulo 1; Capitulo 2 pronto na lista de cenas.");
    }
}
