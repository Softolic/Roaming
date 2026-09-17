using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class ForestElevationBuilder
{
    const string Dir = "Assets/Environment/ForestChapter1/";
    const float ForestY = 1.20f, RoadY = 0.50f, WaterY = -0.32f, BedY = -1.20f;

    static float RoadX(float z)
    {
        // Keep the bridge approach straight, with broad curves elsewhere.
        float curve = Mathf.SmoothStep(0, 1, Mathf.Clamp01((Mathf.Abs(z - 194f) - 25f) / 25f));
        return 4.12f - .08913043f * (z + 55f) + Mathf.Sin((z + 55f) * .038f) * 2.1f * curve;
    }
    static float RiverX(float z)
    {
        float t = Mathf.Clamp01((z - 155f) / 70f);
        t = t * t * (3f - 2f * t);
        return RoadX(z) + Mathf.Lerp(14f + Mathf.Sin(z * .045f) * 2.5f,
            -13f + Mathf.Sin(z * .045f) * 2.5f, t);
    }

    static float Height(float x, float z)
    {
        float dRoad = Mathf.Abs(x - RoadX(z));
        float dRiver = Mathf.Abs(x - RiverX(z));
        float road = dRoad <= 3.2f ? RoadY : dRoad < 5.8f
            ? Mathf.SmoothStep(RoadY, ForestY, (dRoad - 3.2f) / 2.6f) : ForestY;
        float river = dRiver <= 3.4f ? BedY : dRiver < 6.2f
            ? Mathf.SmoothStep(BedY, ForestY, (dRiver - 3.4f) / 2.8f) : ForestY;
        return Mathf.Min(road, river);
    }

    static Mesh MeshAsset(string path)
    {
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh == null) { mesh = new Mesh(); AssetDatabase.CreateAsset(mesh, path); }
        else mesh.Clear();
        return mesh;
    }

    static Material MaterialAsset(string path, Color color)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            mat = new Material(shader); AssetDatabase.CreateAsset(mat, path);
        }
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color); else mat.color = color;
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", .18f);
        return mat;
    }

    static GameObject Child(Transform parent, string name)
    {
        Transform found = parent.Find(name);
        if (found != null) return found.gameObject;
        GameObject go = new GameObject(name); go.transform.SetParent(parent, false); return go;
    }

    static void SetMesh(GameObject go, Mesh mesh, Material material, bool collider)
    {
        MeshFilter filter = go.GetComponent<MeshFilter>(); if (filter == null) filter = go.AddComponent<MeshFilter>();
        MeshRenderer renderer = go.GetComponent<MeshRenderer>(); if (renderer == null) renderer = go.AddComponent<MeshRenderer>();
        filter.sharedMesh = mesh; renderer.sharedMaterial = material;
        if (collider)
        {
            MeshCollider mc = go.GetComponent<MeshCollider>(); if (mc == null) mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = null; mc.sharedMesh = mesh; mc.enabled = true;
        }
    }

    static void BuildTerrain(Transform forest)
    {
        Transform old = forest.Find("Terreno em Niveis"); if (old != null) Object.DestroyImmediate(old.gameObject);
        GameObject root = new GameObject("Terreno em Niveis"); root.transform.SetParent(forest, false);
        float[] starts = {-80, 80, 195, 310}, ends = {80, 195, 310, 425};
        Material fallback = MaterialAsset(Dir + "Terreno_Floresta.mat", new Color(.18f, .24f, .10f));
        Material moss = AssetDatabase.LoadAssetAtPath<Material>(Dir + "Musgo_0.mat") ?? fallback;
        Material dry = AssetDatabase.LoadAssetAtPath<Material>(Dir + "Chao_Seco.mat") ?? fallback;
        Material dead = AssetDatabase.LoadAssetAtPath<Material>(Dir + "Chao_Morto.mat") ?? fallback;
        Material[] mats = {moss, dry, dead, dead};
        for (int s = 0; s < 4; s++)
        {
            int cols = 281, rows = Mathf.RoundToInt(ends[s] - starts[s]) + 1;
            Vector3[] vertices = new Vector3[cols * rows]; Vector2[] uv = new Vector2[vertices.Length];
            int[] triangles = new int[(cols - 1) * (rows - 1) * 6];
            for (int rz = 0; rz < rows; rz++) for (int cx = 0; cx < cols; cx++)
            {
                float x = -70 + cx * .5f, z = starts[s] + rz; int id = rz * cols + cx;
                vertices[id] = new Vector3(x, Height(x, z), z); uv[id] = new Vector2(x * .08f, z * .08f);
            }
            int ti = 0;
            for (int rz = 0; rz < rows - 1; rz++) for (int cx = 0; cx < cols - 1; cx++)
            {
                int a = rz * cols + cx, b = a + 1, c = a + cols, d = c + 1;
                triangles[ti++] = a; triangles[ti++] = c; triangles[ti++] = b;
                triangles[ti++] = b; triangles[ti++] = c; triangles[ti++] = d;
            }
            Mesh mesh = MeshAsset(Dir + "Terreno_Nivel_" + (s + 1) + ".asset");
            mesh.name = "Terreno_Nivel_" + (s + 1); mesh.indexFormat = IndexFormat.UInt32;
            mesh.vertices = vertices; mesh.uv = uv; mesh.triangles = triangles;
            mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds(); EditorUtility.SetDirty(mesh);
            SetMesh(Child(root.transform, "Terreno Nivel " + (s + 1)), mesh, mats[s], true);
        }
    }

    static float BuildRiver(Transform forest)
    {
        const int count = 461; Vector3[] center = new Vector3[count], side = new Vector3[count]; float[] dist = new float[count];
        int crossing = 0; float nearest = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            float z = -55 + i; center[i] = new Vector3(RiverX(z), WaterY, z);
            if (i > 0) dist[i] = dist[i - 1] + Vector3.Distance(center[i], center[i - 1]);
            float gap = Mathf.Abs(RiverX(z) - RoadX(z)); if (gap < nearest) { nearest = gap; crossing = i; }
        }
        for (int i = 0; i < count; i++)
        {
            Vector3 tangent = i == 0 ? center[1] - center[0] : i == count - 1 ? center[count - 1] - center[count - 2] : center[i + 1] - center[i - 1];
            side[i] = Vector3.right; // Same cross sections as the carved riverbed.
        }
        float crossZ = center[crossing].z;
        GameObject river = Child(forest, "Rio da Floresta");
        GameObject water = Child(river.transform, "Agua do Rio"), bed = Child(river.transform, "Leito Visivel");
        GameObject barrier = Child(river.transform, "Limite da Margem"), volume = Child(river.transform, "Volume Profundo");
        GameObject banks = Child(river.transform, "Margens de Terra");
        Renderer bankRenderer = banks.GetComponent<Renderer>(); if (bankRenderer != null) bankRenderer.enabled = false;
        Collider bankCollider = banks.GetComponent<Collider>(); if (bankCollider != null) bankCollider.enabled = false;

        Vector3[] vertices = new Vector3[count * 2]; Vector2[] uv = new Vector2[count * 2]; int[] tri = new int[(count - 1) * 6];
        for (int i = 0; i < count; i++)
        {
            vertices[i * 2] = center[i] - side[i] * 3.4f; vertices[i * 2 + 1] = center[i] + side[i] * 3.4f;
            uv[i * 2] = new Vector2(0, dist[i] * .12f); uv[i * 2 + 1] = new Vector2(1, dist[i] * .12f);
            if (i < count - 1) { int q = i * 6, a = i * 2, b = a + 1, c = a + 2, d = a + 3; tri[q] = a; tri[q+1] = c; tri[q+2] = b; tri[q+3] = b; tri[q+4] = c; tri[q+5] = d; }
        }
        Mesh waterMesh = MeshAsset(Dir + "Rio_Agua_Mesh.asset"); waterMesh.name = "Rio_Agua_Mesh"; waterMesh.indexFormat = IndexFormat.UInt32;
        waterMesh.vertices = vertices; waterMesh.uv = uv; waterMesh.triangles = tri; waterMesh.RecalculateNormals(); waterMesh.RecalculateTangents(); waterMesh.RecalculateBounds(); EditorUtility.SetDirty(waterMesh);
        SetMesh(water, waterMesh, AssetDatabase.LoadAssetAtPath<Material>(Dir + "Rio_Agua.mat"), false);
        Vector3[] bedVertices = (Vector3[])vertices.Clone(); for (int i = 0; i < bedVertices.Length; i++) bedVertices[i].y = -1.18f;
        Mesh bedMesh = MeshAsset(Dir + "Rio_Leito_Mesh.asset"); bedMesh.name = "Rio_Leito_Mesh"; bedMesh.indexFormat = IndexFormat.UInt32;
        bedMesh.vertices = bedVertices; bedMesh.uv = uv; bedMesh.triangles = tri; bedMesh.RecalculateNormals(); bedMesh.RecalculateTangents(); bedMesh.RecalculateBounds(); EditorUtility.SetDirty(bedMesh);
        SetMesh(bed, bedMesh, AssetDatabase.LoadAssetAtPath<Material>(Dir + "Rio_Leito.mat"), false);

        List<Vector3> wallV = new List<Vector3>(); List<int> wallT = new List<int>();
        for (int edge = -1; edge <= 1; edge += 2)
        for (int i = 0; i < count - 1; i++)
        {
            if (Mathf.Abs((center[i].z + center[i+1].z) * .5f - crossZ) < 24) continue;
            Vector3 a = center[i] + side[i] * (edge * 5.1f), b = center[i+1] + side[i+1] * (edge * 5.1f); a.y = b.y = -1.25f;
            Vector3 c = a, d = b; c.y = d.y = 1.55f; int k = wallV.Count;
            wallV.Add(a); wallV.Add(b); wallV.Add(c); wallV.Add(d);
            wallT.AddRange(new[]{k,k+2,k+1,k+1,k+2,k+3,k+1,k+2,k,k+3,k+2,k+1});
        }
        Mesh wall = MeshAsset(Dir + "Rio_Limite_Mesh.asset"); wall.name = "Rio_Limite_Mesh"; wall.indexFormat = IndexFormat.UInt32;
        wall.vertices = wallV.ToArray(); wall.triangles = wallT.ToArray(); wall.RecalculateNormals(); wall.RecalculateBounds(); EditorUtility.SetDirty(wall);
        SetMesh(barrier, wall, null, true); barrier.GetComponent<MeshRenderer>().enabled = false;

        List<Vector3> volV = new List<Vector3>(); List<int> volT = new List<int>();
        for (int i = 0; i < count; i++)
        {
            Vector3 l = center[i] - side[i] * 3.4f, r = center[i] + side[i] * 3.4f;
            volV.Add(new Vector3(l.x, WaterY-.02f, l.z)); volV.Add(new Vector3(r.x, WaterY-.02f, r.z));
            volV.Add(new Vector3(l.x, BedY, l.z)); volV.Add(new Vector3(r.x, BedY, r.z));
        }
        for (int i = 0; i < count - 1; i++)
        {
            int a=i*4,b=a+1,c=a+2,d=a+3,e=a+4,f=a+5,g=a+6,h=a+7;
            volT.AddRange(new[]{c,g,d,d,g,h,a,c,e,e,c,g,b,f,d,d,f,h});
        }
        Mesh volumeMesh = MeshAsset(Dir + "Rio_Profundidade_Mesh.asset"); volumeMesh.name = "Rio_Profundidade_Mesh"; volumeMesh.indexFormat = IndexFormat.UInt32;
        volumeMesh.vertices = volV.ToArray(); volumeMesh.triangles = volT.ToArray(); volumeMesh.RecalculateNormals(); volumeMesh.RecalculateBounds(); EditorUtility.SetDirty(volumeMesh);
        SetMesh(volume, volumeMesh, AssetDatabase.LoadAssetAtPath<Material>(Dir + "Rio_Leito.mat"), false);
        Collider volumeCollider = volume.GetComponent<Collider>(); if (volumeCollider != null) volumeCollider.enabled = false;
        return crossZ;
    }

    static void BuildBridge(Transform forest, float crossZ)
    {
        Transform old = forest.Find("Ponte de Madeira"); if (old != null) Object.DestroyImmediate(old.gameObject);
        GameObject bridge = new GameObject("Ponte de Madeira"); bridge.transform.SetParent(forest, false);
        bridge.transform.position = new Vector3(RoadX(crossZ), 0, crossZ);
        Vector3 tangent = new Vector3(RoadX(crossZ+1)-RoadX(crossZ-1),0,2).normalized;
        bridge.transform.rotation = Quaternion.Euler(0, Mathf.Atan2(tangent.x,tangent.z)*Mathf.Rad2Deg, 0);
        Material wood = MaterialAsset(Dir+"Ponte_Madeira.mat",new Color(.30f,.14f,.055f));
        Material light = MaterialAsset(Dir+"Ponte_Madeira_Clara.mat",new Color(.37f,.20f,.095f));
        Material iron = MaterialAsset(Dir+"Ponte_Ferragens.mat",new Color(.13f,.14f,.14f));
        Material stone = MaterialAsset(Dir+"Ponte_Pedra.mat",new Color(.32f,.31f,.27f));
        const int boards=72;
        for(int i=0;i<boards;i++)
        {
            float z=-18+(i+.5f)*.5f;
            GameObject board=Cube(bridge.transform,"Tabua "+(i+1),new Vector3(0,DeckY(z)-.075f,z),new Vector3(5.8f,.15f,.485f),i%4==0?light:wood);
            float slope=(DeckY(z+.24f)-DeckY(z-.24f))/.48f;
            board.transform.localRotation=Quaternion.Euler(-Mathf.Atan(slope)*Mathf.Rad2Deg,0,0);
            board.GetComponent<BoxCollider>().enabled=false;
        }
        // One continuous surface supplies smooth walking over all planks and ramps.
        Vector3[] vertices=new Vector3[146]; int[] triangles=new int[432];
        for(int i=0;i<=72;i++)
        {
            float z=-18+i*.5f; vertices[i*2]=new Vector3(-2.9f,DeckY(z)+.015f,z);vertices[i*2+1]=new Vector3(2.9f,DeckY(z)+.015f,z);
            if(i<72){int a=i*2,q=i*6;triangles[q]=a;triangles[q+1]=a+2;triangles[q+2]=a+1;triangles[q+3]=a+1;triangles[q+4]=a+2;triangles[q+5]=a+3;}
        }
        Mesh deck=MeshAsset(Dir+"Ponte_Piso_Colisao.asset");deck.name="Ponte_Piso_Colisao";deck.vertices=vertices;deck.triangles=triangles;deck.RecalculateNormals();deck.RecalculateBounds();EditorUtility.SetDirty(deck);
        GameObject deckObject=Child(bridge.transform,"Piso Continuo");deckObject.AddComponent<MeshCollider>().sharedMesh=deck;
        for(int side=-1;side<=1;side+=2)
        {
            for(int i=0;i<=10;i++)
            {
                float z=-17.5f+i*3.5f,y=DeckY(z),x=side*2.76f;
                Cube(bridge.transform,"Pilar do Corrimao",new Vector3(x,y+.57f,z),new Vector3(.22f,1.25f,.22f),wood);
                Cube(bridge.transform,"Chapeu do Pilar",new Vector3(x,y+1.24f,z),new Vector3(.32f,.12f,.32f),light);
                Cube(bridge.transform,"Cinta de Ferro",new Vector3(x,y+.2f,z),new Vector3(.235f,.11f,.235f),iron);
                if(i==10)continue;
                float z2=z+3.5f,y2=DeckY(z2);
                Beam(bridge.transform,"Corrimao",new Vector3(x,y+1.05f,z),new Vector3(x,y2+1.05f,z2),.15f,wood);
                Beam(bridge.transform,"Travessa Inferior",new Vector3(x,y+.22f,z),new Vector3(x,y2+.22f,z2),.13f,wood);
                Beam(bridge.transform,"Diagonal X",new Vector3(x,y+.28f,z+.12f),new Vector3(x,y2+.95f,z2-.12f),.1f,light);
                Beam(bridge.transform,"Diagonal X",new Vector3(x,y+.95f,z+.12f),new Vector3(x,y2+.28f,z2-.12f),.1f,light);
                Beam(bridge.transform,"Longarina",new Vector3(side*2.2f,y-.24f,z),new Vector3(side*2.2f,y2-.24f,z2),.28f,wood);
                GameObject guard=Cube(bridge.transform,"Colisao Lateral",new Vector3(x,(y+y2)*.5f+.62f,(z+z2)*.5f),new Vector3(.17f,1.25f,3.5f),wood);guard.GetComponent<Renderer>().enabled=false;
            }
            for(int i=0;i<5;i++)
            {
                float z=-14+i*7; Vector3 world=bridge.transform.TransformPoint(new Vector3(side*2.18f,0,z));
                float bottom=Height(world.x,world.z)-.2f,top=DeckY(z)-.15f;
                Cube(bridge.transform,"Estaca de Apoio",new Vector3(side*2.18f,(bottom+top)*.5f,z),new Vector3(.4f,Mathf.Max(.25f,top-bottom),.4f),wood);
                Cube(bridge.transform,"Base de Pedra",new Vector3(side*2.18f,bottom+.12f,z),new Vector3(.68f,.25f,.68f),stone);
            }
        }
    }

    static float DeckY(float z)
    {
        float t=Mathf.Clamp01((18-Mathf.Abs(z))/4f);
        return Mathf.Lerp(.55f,.90f,Mathf.SmoothStep(0,1,t))+.18f*Mathf.Pow(Mathf.Cos(z/18f*Mathf.PI*.5f),2);
    }

    static GameObject Cube(Transform parent,string name,Vector3 position,Vector3 scale,Material material)
    {
        var mesh=UnityEngine.ProBuilder.ShapeGenerator.GenerateCube(UnityEngine.ProBuilder.PivotLocation.Center,scale);
        GameObject go=mesh.gameObject;go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;
        go.GetComponent<Renderer>().sharedMaterial=material;
        BoxCollider collider=go.GetComponent<BoxCollider>();if(collider==null)collider=go.AddComponent<BoxCollider>();collider.size=scale;
        return go;
    }

    static void Beam(Transform parent,string name,Vector3 a,Vector3 b,float width,Material material)
    {
        var go=Cube(parent,name,(a+b)*.5f,new Vector3(width,width,Vector3.Distance(a,b)),material);
        go.transform.localRotation=Quaternion.LookRotation(b-a,Vector3.up);
    }

    static void BuildRoad(Transform forest,float crossZ)
    {
        GameObject root=Child(forest,"Estrada da Floresta");
        Material dirt=MaterialAsset(Dir+"Estrada_Terra.mat",new Color(.40f,.31f,.19f));
        Material shoulder=MaterialAsset(Dir+"Estrada_Acostamento.mat",new Color(.30f,.26f,.16f));
        // The earth stops at the bridge abutments; the river stays open underneath.
        float halfZ=18f/Mathf.Sqrt(1+.08913043f*.08913043f);
        float[] starts={-55,crossZ+halfZ},ends={crossZ-halfZ,405};
        for(int part=0;part<2;part++)
        {
            int rows=Mathf.CeilToInt((ends[part]-starts[part])*2)+1;
            Vector3[] v=new Vector3[rows*6];Vector2[] uv=new Vector2[v.Length];
            List<int> center=new List<int>(),edge=new List<int>();
            for(int i=0;i<rows;i++)
            {
                float z=Mathf.Lerp(starts[part],ends[part],i/(float)(rows-1)),x=RoadX(z);
                float taper=Mathf.SmoothStep(0,1,Mathf.Clamp01((Mathf.Abs(z-crossZ)-18)/8));
                float halfWidth=Mathf.Lerp(2.9f,2.45f+Mathf.Sin(z*.075f)*.13f,taper);
                float[] offsets={-halfWidth-.48f,-halfWidth,-.7f,.7f,halfWidth,halfWidth+.48f};
                for(int col=0;col<6;col++)
                {
                    float px=x+offsets[col];float y=col==0||col==5?Mathf.Max(Height(px,z)+.022f,.515f):.55f;
                    v[i*6+col]=new Vector3(px,y,z);uv[i*6+col]=new Vector2(offsets[col]*.2f,z*.2f);
                }
                if(i==rows-1)continue;
                for(int col=0;col<5;col++){int a=i*6+col;var tris=col==0||col==4?edge:center;tris.AddRange(new[]{a,a+6,a+1,a+1,a+6,a+7});}
            }
            Mesh mesh=MeshAsset(Dir+"Estrada_Trecho_"+part+".asset");mesh.name="Estrada_Trecho_"+part;
            mesh.vertices=v;mesh.uv=uv;mesh.subMeshCount=2;mesh.SetTriangles(center,0);mesh.SetTriangles(edge,1);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
            GameObject go=Child(root.transform,part==0?"Estrada ate a Ponte":"Estrada apos a Ponte");SetMesh(go,mesh,dirt,true);go.GetComponent<Renderer>().sharedMaterials=new[]{dirt,shoulder};
        }
    }

    static void FitVegetationAndBushCollisions(Transform forest)
    {
        GameObject collisions=Child(forest,"Colisao Solida dos Arbustos");
        for(int i=collisions.transform.childCount-1;i>=0;i--)Object.DestroyImmediate(collisions.transform.GetChild(i).gameObject);
        int count=0;
        foreach(Transform group in forest)
        {
            bool bush=group.name.StartsWith("Arbustos");
            if(!bush&&!group.name.StartsWith("Pinheiros")&&!group.name.StartsWith("Tocos Cortados"))continue;
            foreach(Transform plant in group)
            {
                Renderer[] renderers=plant.GetComponentsInChildren<Renderer>();if(renderers.Length==0)continue;
                Bounds bounds=renderers[0].bounds;foreach(Renderer renderer in renderers)bounds.Encapsulate(renderer.bounds);
                Vector3 p=plant.position;float radius=Mathf.Min(2,Mathf.Max(bounds.extents.x,bounds.extents.z));
                float offset=p.x-RoadX(p.z),safe=3.9f+radius;
                if(Mathf.Abs(offset)<safe){p.x=RoadX(p.z)+(offset>=0?1:-1)*safe;plant.position=p;}
                // Keep the base seated in the slopes, including shrubs moved beside the path.
                bounds=renderers[0].bounds;foreach(Renderer renderer in renderers)bounds.Encapsulate(renderer.bounds);
                p=plant.position;p.y+=Height(p.x,p.z)-bounds.min.y-(group.name.StartsWith("Tocos")?.1f:.035f);plant.position=p;
                if(!bush)continue;
                bounds=renderers[0].bounds;foreach(Renderer renderer in renderers)bounds.Encapsulate(renderer.bounds);
                GameObject solid=new GameObject(group.name+" - "+plant.name);solid.transform.SetParent(collisions.transform,false);
                solid.transform.position=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);solid.layer=plant.gameObject.layer;
                CapsuleCollider capsule=solid.AddComponent<CapsuleCollider>();capsule.direction=1;
                capsule.radius=Mathf.Max(.12f,Mathf.Min(bounds.extents.x,bounds.extents.z)*.80f);
                capsule.height=Mathf.Max(capsule.radius*2,bounds.size.y*.88f);capsule.center=new Vector3(0,capsule.height*.5f,0);capsule.isTrigger=false;
                // Original trigger and ArbustoVivo remain on the visual for touch reactions.
                foreach(Collider trigger in plant.GetComponentsInChildren<Collider>())if(trigger.isTrigger)trigger.enabled=true;
                count++;
            }
        }
        Debug.Log("[Floresta] Arbustos com colisao solida: "+count);
    }

    [MenuItem("Roaming/Construir desniveis rio e ponte")]
    public static void Build()
    {
        Scene scene=SceneManager.GetActiveScene(); if(scene.path!="Assets/Scenes/Capitulo 1.unity") scene=EditorSceneManager.OpenScene("Assets/Scenes/Capitulo 1.unity",OpenSceneMode.Single);
        GameObject forest=GameObject.Find("Floresta - Capitulo 1"); if(forest==null) throw new System.Exception("Floresta - Capitulo 1 nao encontrada");
        string[] floors={"Solo e Trilha","Solo e Trilha - Extensao 2","Solo e Trilha - Extensao 3","Solo e Trilha - Extensao 4"};
        foreach(string name in floors)
        {
            Transform group=forest.transform.Find(name); if(group==null)continue;
            Transform floor=group.Find("Chao da Floresta"); if(floor!=null){Renderer r=floor.GetComponent<Renderer>();if(r!=null)r.enabled=false;Collider c=floor.GetComponent<Collider>();if(c!=null)c.enabled=false;}
            Transform trail=group.Find("Trilha ate a Clareira"); if(trail!=null)trail.gameObject.SetActive(false);
        }
        BuildTerrain(forest.transform); float crossZ=BuildRiver(forest.transform);
        string[] groups={"Pinheiros","Pinheiros - Extensao 2","Pinheiros - Extensao 3","Arbustos","Arbustos - Extensao 2","Arbustos - Extensao 3","Arbustos - Extensao 4","Tocos Cortados - Final","Tocos Cortados - Trecho Extra"};
        foreach(string name in groups)
        {
            Transform group=forest.transform.Find(name);if(group==null)continue;Vector3 gp=group.localPosition;gp.y=ForestY;group.localPosition=gp;
            float clear=name.StartsWith("Pinheiros")?6.6f:name.StartsWith("Arbustos")?5.5f:5.4f;
            for(int i=group.childCount-1;i>=0;i--){Transform child=group.GetChild(i);Vector3 p=child.position;if(Mathf.Abs(p.x-RiverX(p.z))<clear)Object.DestroyImmediate(child.gameObject);}
        }
        BuildBridge(forest.transform,crossZ);
        BuildRoad(forest.transform,crossZ);
        FitVegetationAndBushCollisions(forest.transform);
        GameObject player=GameObject.Find("Toby");if(player==null)try{player=GameObject.FindGameObjectWithTag("Player");}catch{}
        if(player!=null) ConfigureSafeSpawn(forest.transform,player);
        GameObject ball=GameObject.Find("Bolinha Vermelha");if(ball!=null&&(player==null||!ball.transform.IsChildOf(player.transform))){Vector3 p=ball.transform.position;p.y=Height(p.x,p.z)+.18f;ball.transform.position=p;}
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);SceneView.RepaintAll();
        Debug.Log("[Floresta] Desniveis, rio cruzando a trilha e ponte criados em z="+crossZ.ToString("F1"));
    }

    public static void ConfigureSafeSpawn(Transform forest,GameObject player)
    {
        var surfaces=new List<Collider>();
        string[] roots={"Terreno em Niveis","Estrada da Floresta","Ponte de Madeira/Piso Continuo"};
        foreach(string path in roots){Transform root=forest.Find(path);if(root!=null)surfaces.AddRange(root.GetComponentsInChildren<Collider>());}
        var grounding=player.GetComponent<ForestSpawnGrounding>();if(grounding==null)grounding=player.AddComponent<ForestSpawnGrounding>();
        grounding.Configure(surfaces.ToArray());
        Vector3 p=player.transform.position;p.x=RoadX(p.z);p.y=0;
        player.transform.position=p;player.GetComponent<Rigidbody>().position=p;
        Physics.SyncTransforms();grounding.PlaceAboveGround();
        Transform visual=player.transform.Find("Toby");
        var capsule=player.GetComponent<CapsuleCollider>();
        if(visual!=null&&capsule!=null)
        {
            float lowestToe=float.PositiveInfinity;
            foreach(Transform bone in visual.GetComponentsInChildren<Transform>())
                if(bone.name.StartsWith("DEF")&&bone.name.Contains("foot")&&bone.name.EndsWith(".001"))
                    lowestToe=Mathf.Min(lowestToe,bone.position.y);
            if(!float.IsPositiveInfinity(lowestToe))
                visual.position+=Vector3.up*(capsule.bounds.min.y+.025f-lowestToe);
        }
        EditorUtility.SetDirty(grounding);EditorUtility.SetDirty(player);
    }
}
