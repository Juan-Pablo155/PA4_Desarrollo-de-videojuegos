using UnityEngine;

public class PA3Ayuda : MonoBehaviour
{
    FirstPersonController control;
    Vector3 inicio;
    void Awake() { control=GetComponent<FirstPersonController>(); inicio=transform.position; }
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape)) { Cursor.lockState=CursorLockMode.None; Cursor.visible=true; control.cameraCanMove=false; control.playerCanMove=false; }
        if(Input.GetMouseButtonDown(0)) { Cursor.lockState=CursorLockMode.Locked; Cursor.visible=false; control.cameraCanMove=true; control.playerCanMove=true; }
        if(Input.GetKeyDown(KeyCode.R) || transform.position.y < -10) { var rb=GetComponent<Rigidbody>(); rb.position=inicio; rb.linearVelocity=Vector3.zero; }
    }
    void OnGUI() { GUI.Box(new Rect(15,15,465,82),"EXPLORACION DEL TERRENO | PA3\nWASD: caminar | Raton: mirar | Espacio: saltar\nShift: correr | E: mineral | R: volver | Esc: cursor"); }
}
