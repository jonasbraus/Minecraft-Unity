using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DragAndDropHandler : MonoBehaviour
{
    public UIItemSlot cursorSlot = null;
    public ItemSlot cursorItemSlot;

    public GraphicRaycaster raycaster = null;
    private PointerEventData pointerEventData;
    public EventSystem eventSystem = null;

    private World world;
    
    private void Start()
    {
        world = GameObject.Find("World").GetComponent<World>();
        
        cursorItemSlot = new ItemSlot(cursorSlot);
    }

    private void Update()
    {
        if (!world.inUI)
        {
            return;
        }

        cursorSlot.transform.position = Input.mousePosition;

        if (Input.GetMouseButtonDown(0))
        {
            if (CheckForSlot() != null)
            {
                HandleSlotClick(CheckForSlot());
            }
            else
            {
                cursorItemSlot.EmptySlot();
            }
        }
    }

    private void HandleSlotClick(UIItemSlot clickedSlot)
    {
        if (clickedSlot == null)
        {
            return;
        }

        if (!cursorSlot.HasItem && !clickedSlot.HasItem)
        {
            return;
        }
        
        
        if (clickedSlot.itemSlot.isCreative)
        {
            cursorItemSlot.EmptySlot();
            cursorItemSlot.InsertStack(clickedSlot.itemSlot.stack);
            return;
        }

        if (!cursorSlot.HasItem && clickedSlot.HasItem)
        {
            cursorItemSlot.InsertStack(clickedSlot.itemSlot.TakeAll());
            return;
        }
        
        if (cursorSlot.HasItem && !clickedSlot.HasItem)
        {
            clickedSlot.itemSlot.InsertStack(cursorItemSlot.TakeAll());
            return;
        }
        
        if (cursorSlot.HasItem && clickedSlot.HasItem)
        {
            if (cursorSlot.itemSlot.stack.id != clickedSlot.itemSlot.stack.id)
            {
                ItemStack oldCursorSlot = cursorSlot.itemSlot.TakeAll();
                ItemStack oldSlot = clickedSlot.itemSlot.TakeAll();

                clickedSlot.itemSlot.InsertStack(oldCursorSlot);
                cursorItemSlot.InsertStack(oldSlot);
            }
        }
    }

    private UIItemSlot CheckForSlot()
    {
        pointerEventData = new PointerEventData(eventSystem);
        pointerEventData.position = Input.mousePosition;

        List<RaycastResult> result = new List<RaycastResult>();
        raycaster.Raycast(pointerEventData, result);

        foreach (RaycastResult res in result)
        {
            if (res.gameObject.tag == "UiItemSlot")
            {
                return res.gameObject.GetComponent<UIItemSlot>();
            }
        }

        return null;
    }
}
