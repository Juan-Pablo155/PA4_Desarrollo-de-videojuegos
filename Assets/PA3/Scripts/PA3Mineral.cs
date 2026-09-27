using UnityEngine;

// Interaccion añadida a la roca mineral que ya estaba en la recuperacion.
public class PA3Mineral : MonoBehaviour
{
    public ParticleSystem efecto;
    public Light luz;
    public Transform jugador;
    bool dentro, activo;
    void OnTriggerEnter(Collider c) { if (c.GetComponentInParent<FirstPersonController>()) dentro = true; }
    void OnTriggerExit(Collider c) { if (c.GetComponentInParent<FirstPersonController>()) dentro = false; }
    void Update()
    {
        if (!dentro || Cursor.lockState != CursorLockMode.Locked || !Input.GetKeyDown(KeyCode.E)) return;
        activo = !activo; luz.enabled = activo;
        if (activo) efecto.Play(); else efecto.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }
    void OnGUI()
    {
        if (dentro) GUI.Box(new Rect(Screen.width/2-190,Screen.height-100,380,60),activo ? "MINERAL ACTIVADO\nE: apagar la energia" : "ROCA MINERAL\nE: activar la energia");
    }
}
