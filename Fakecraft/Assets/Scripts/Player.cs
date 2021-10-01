using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public GameObject CreativeInventory;
    public DragAndDropHandler dragAndDropHandler;
    
    public bool isGrounded;
    public bool isSprinting;
    
    public float walkSpeed = 4;
    public float sprintSpeed = 6;
    public float jumpForce = 6;

    public float playerWidth = .15f;
    public float playerOffsetWidth = .1f;
    public Transform highlightBlock;
    
    private World world;
    public float gravity = -20f;

    private Camera cam;
    private Transform camTransform;
    private float checkIncrement = .1f;
    public float reach = 8;
    
    private float horizontal;
    private float vertical;
    private float mouseX;
    private float mouseY;
    private Vector3 velocity = new Vector3(0, 0, 0);
    private float rotX = 0;
    private float verticalMomentum = 0;
    private bool jumpRequest;

    private Vector3 placePosition = new Vector3(), destroyPosition = new Vector3();
    private float lastBlockAction = 0;

    public Toolbar toolbar;

    private void Start()
    {
        world = GameObject.Find("World").GetComponent<World>();
        Cursor.lockState = CursorLockMode.Locked;
        cam = GetComponentInChildren<Camera>();
        camTransform = cam.transform;
        lastBlockAction = Time.time;
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            world.inUI = !world.inUI;
            if (!world.inUI)
            {
                Cursor.lockState = CursorLockMode.Locked;
                CreativeInventory.SetActive(false);
                dragAndDropHandler.cursorItemSlot.EmptySlot();
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                CreativeInventory.SetActive(true);
            }
        }
        
        if(!world.inUI)
        {
            GetPlayerInputs();

            rotX -= mouseY * 1.4f;
            rotX = Mathf.Clamp(rotX, -90, 90);

            camTransform.localRotation = Quaternion.Euler(rotX, 0, 0);

            transform.Rotate(0, mouseX, 0);

            PlaceCursorBlocks();
        }
    }

    private void FixedUpdate()
    {
        if(!world.inUI)
        {
            if (jumpRequest)
            {
                Jump();
            }

            CalculateVelocity();


            transform.Translate(velocity, Space.World);
        }
    }

    private void CalculateVelocity()
    {
        //affect vertical momentum with gravity
        if (verticalMomentum > gravity)
        {
            verticalMomentum += Time.fixedDeltaTime * gravity;
        }
        
        //if we are sprinting
        if (isSprinting)
        {
            velocity = ((transform.forward * vertical) + (transform.right * horizontal)) * Time.fixedDeltaTime * sprintSpeed;
        }
        else
        {
            velocity = ((transform.forward * vertical) + (transform.right * horizontal)) * Time.fixedDeltaTime * walkSpeed;
        }
        
        //apply vertical momentum (falling / jumping)
        velocity += Vector3.up * verticalMomentum * Time.fixedDeltaTime;

        //check for forward and backward
        if ((velocity.z > 0 && front) || (velocity.z < 0 && back))
        {
            velocity.z = 0;
        }

        //check for left and right
        if ((velocity.x > 0 && right) || (velocity.x < 0 && left))
        {
            velocity.x = 0;
        }

        //check for falling and jumping
        if (velocity.y < 0)
        {
            velocity.y = CheckDownSpeed(velocity.y);
        }
        else if (velocity.y > 0)
        {
            velocity.y = CheckUpSpeed(velocity.y);
        }
        else
        {
            velocity.y = 0;
        }
        
    }

    private void Jump()
    {
        verticalMomentum = jumpForce;
        isGrounded = false;
        jumpRequest = false;
    }

    private void GetPlayerInputs()
    {
        horizontal = Input.GetAxisRaw("Horizontal");
        vertical = Input.GetAxisRaw("Vertical");
        mouseX = Input.GetAxis("Mouse X");
        mouseY = Input.GetAxis("Mouse Y");

        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            isSprinting = true;
        }

        if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            isSprinting = false;
        }

        if (isGrounded && Input.GetButton("Jump"))
        {
            jumpRequest = true;
        }

        if(highlightBlock.gameObject.activeSelf)
        {
            Vector3 playerPos = new Vector3((int) transform.position.x, (int) transform.position.y, (int) transform.position.z);
            
            if (Input.GetMouseButton(0) && Time.time - lastBlockAction > 0.22f || Input.GetMouseButtonDown(0))
            {
                world.GetChunkFromVector3(destroyPosition).EditVoxel(destroyPosition, 0);
                lastBlockAction = Time.time;
            }

            if (Input.GetMouseButton(1) && Time.time - lastBlockAction > 0.3f || Input.GetMouseButtonDown(1))
            {
                if(!placePosition.Equals(playerPos) && !placePosition.Equals(playerPos + Vector3.up))
                {
                    if(toolbar.slots[toolbar.slotIndex].HasItem)
                    {
                        world.GetChunkFromVector3(placePosition).EditVoxel(placePosition, toolbar.slots[toolbar.slotIndex].itemSlot.stack.id);
                        lastBlockAction = Time.time;
                        toolbar.slots[toolbar.slotIndex].itemSlot.Take(1);
                    }
                }
            }
        }
    }

    //function like raycast
    private void PlaceCursorBlocks()
    {
        float step = checkIncrement;
        Vector3 lastPos = new Vector3();
        while (step < reach)
        {
            Vector3 pos = camTransform.position + (camTransform.forward * step);

            if (world.CheckForVoxel(pos))
            {
                destroyPosition = new Vector3((int) pos.x, (int) pos.y, (int) pos.z);
                placePosition = lastPos;
                
                highlightBlock.gameObject.SetActive(true);
                highlightBlock.position = destroyPosition;
                return;
            }
            
            lastPos = new Vector3((int) pos.x, (int) pos.y, (int) pos.z);
            step += checkIncrement;
        }
        
        highlightBlock.gameObject.SetActive(false);
    }

    private float CheckDownSpeed(float downSpeed)
    {
        if (
            world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + downSpeed, transform.position.z - playerWidth)) ||
            world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + downSpeed, transform.position.z - playerWidth)) ||
            world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + downSpeed, transform.position.z + playerWidth)) ||
            world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + downSpeed, transform.position.z + playerWidth))
        )
        {
            isGrounded = true;
            return 0;
        }

        isGrounded = false;
        return downSpeed;
    }
    
    private float CheckUpSpeed(float upSpeed)
    {
        if (
            world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + upSpeed + 2f, transform.position.z - playerWidth)) ||
            world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + upSpeed + 2f, transform.position.z - playerWidth)) ||
            world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + upSpeed + 2f, transform.position.z + playerWidth)) ||
            world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + upSpeed + 2f, transform.position.z + playerWidth))
        )
        {
            return 0;
        }
        
        return upSpeed;
    }

    public bool front
    {
        get
        {
            if (
                world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y, transform.position.z + playerWidth + playerOffsetWidth)) ||
                world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z + playerWidth + playerOffsetWidth))
            )
            {
                return true;
            }

            return false;
        }
    }
    
    public bool back
    {
        get
        {
            if (
                world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y, transform.position.z - playerWidth - playerOffsetWidth)) ||
                world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z - playerWidth - playerOffsetWidth))
            )
            {
                return true;
            }

            return false;
        }
    }
    
    public bool left
    {
        get
        {
            if (
                world.CheckForVoxel(new Vector3(transform.position.x - playerWidth - playerOffsetWidth, transform.position.y, transform.position.z )) ||
                world.CheckForVoxel(new Vector3(transform.position.x - playerWidth - playerOffsetWidth, transform.position.y + 1f, transform.position.z))
            )
            {
                return true;
            }

            return false;
        }
    }
    
    public bool right
    {
        get
        {
            if (
                world.CheckForVoxel(new Vector3(transform.position.x + playerWidth + playerOffsetWidth, transform.position.y, transform.position.z)) ||
                world.CheckForVoxel(new Vector3(transform.position.x + playerWidth + playerOffsetWidth, transform.position.y + 1f, transform.position.z))
            )
            {
                return true;
            }

            return false;
        }
    }
}
