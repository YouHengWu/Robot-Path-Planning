using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseEvent : MonoBehaviour
{
    public Vector3 offset;
    public Vector3 ScreenPoint;
    
    public float Find_Angle(Vector2 New, Vector2 Old)
    {
        return 180 / Mathf.PI * Mathf.Atan2(New.y, New.x) - Mathf.Atan2(Old.y, Old.x);
    }
    void OnMouseOver()
    {
        RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);

        if (Input.GetMouseButtonDown(0))
        {
            if (hit.collider)
            {
                ScreenPoint = Camera.main.WorldToScreenPoint(transform.parent.position);
                offset = transform.parent.position - Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, ScreenPoint.z));                       
            }
        }
    }
    void OnMouseDrag()
    {

        Vector3 Screen = Camera.main.WorldToScreenPoint(transform.parent.position);
        Vector3 Mouse_Position = new Vector3(Input.mousePosition.x, Input.mousePosition.y, Screen.z);
        Vector3 New_Position = Camera.main.ScreenToWorldPoint(Mouse_Position) + offset;

        Vector2 Rotate_Old = new Vector2 (Mouse_Position.x - transform.parent.position.x, Mouse_Position.y - transform.parent.position.y);
        Vector2 Rotate_New = new Vector2 (New_Position.x - transform.parent.position.x, New_Position.y - transform.parent.position.y);

        if (Input.GetKey("t"))
        {                
            transform.parent.position = new Vector2(Mathf.Clamp(New_Position.x, 0, 128), Mathf.Clamp(New_Position.y, 0, 128));
        }
        else if (Input.GetKey("r"))
        {
            transform.parent.rotation = Quaternion.Euler(0, 0, Find_Angle(Rotate_New, Rotate_Old));
        }

    }
}