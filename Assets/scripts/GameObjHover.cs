using UnityEngine;
using UnityEngine.Events;

public class GameObjHover : MonoBehaviour
{
   [SerializeField] private UnityEvent onHoverEnter;
   [SerializeField] private UnityEvent onHoverExit;
   private void OnMouseEnter()
    {
        onHoverEnter.Invoke();
        // Change the color of the object when the mouse hovers over it
        //MatrialRed();
    }

    public void MatrialRed()
    {
        GetComponent<Renderer>().material.color = Color.red;
    }

    private void OnMouseExit()
    {
        onHoverExit.Invoke();
        // Change the color of the object back to its original color when the mouse exits it
       // MatrialWhite();
    }

    public void MatrialWhite()
    {
        GetComponent<Renderer>().material.color = Color.white;
    }
}
