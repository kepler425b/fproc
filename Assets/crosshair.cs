using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class crosshair : MonoBehaviour {
    public Material lineMat = new Material("Shader \"Lines/Colored Blended\" {" + "SubShader { Pass { " + "    Blend SrcAlpha OneMinusSrcAlpha " + "    ZWrite Off Cull Off Fog { Mode Off } " + "    BindChannels {" + "      Bind \"vertex\", vertex Bind \"color\", color }" + "} } }");
    public float crosshar_width = 2f;

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    void OnPostRender()
    {
        Vector3 A = new Vector3(transform.position.x - crosshar_width,
           transform.position.y,
           transform.position.z);
        Vector3 B = new Vector3(transform.position.x + crosshar_width,
            transform.position.y,
            transform.position.z);
        Vector3 C = new Vector3(transform.position.x,
            transform.position.y + crosshar_width,
            transform.position.z);
        Vector3 D = new Vector3(transform.position.x,
            transform.position.y - crosshar_width,
            transform.position.z);

        GL.PushMatrix();
        GL.LoadIdentity();
        lineMat.SetPass(0);
        GL.Color(Color.white);
        GL.Vertex(A);
        GL.Vertex(B);
        GL.Vertex(C);
        GL.Vertex(D);
        GL.End();
        GL.PopMatrix();

    }

}
