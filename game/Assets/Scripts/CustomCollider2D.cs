﻿using System;
using System.Collections.Generic;
using UnityEngine;

public enum ColliderType
{
    Sphere,
    AABB
}

public class CustomCollider2D : MonoBehaviour
{
    private List<CustomCollider2D> customCollider2DsCollissionning;

    public ColliderType ColliderType;

    public event Action<CustomCollision> OnCollisionEnter2D;
    public event Action<CustomCollision> OnCollisionExit2D;

    public float width = 0.0f;
    public float heigth = 0.0f;

    public float radius = 0.0f;

    public Vector3 centerOffset;
    private Vector3 prevPosition;

    public Vector2 Center
    {
        get { return transform.position + centerOffset; }
    }

    public Vector2 PrevCenter
    {
        get { return prevPosition; }
    }

#if DEBUG
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;

        switch (ColliderType)
        {
            case ColliderType.Sphere:
                {
                    Gizmos.DrawWireSphere(transform.position + centerOffset, radius);
                }
                break;
            case ColliderType.AABB:
                {
                    Gizmos.DrawWireCube(transform.position + centerOffset, new Vector3(width, heigth, 0));
                }
                break;
            default:
                break;
        }
    }
#endif

    private void Awake()
    {
        CollisionManager.GetInstance().RegisterCollider(this);
        
        OnCollisionEnter2D += (CustomCollision col) =>
        {
            Debug.Log("CollisionEnter!");
        };

        OnCollisionExit2D += (CustomCollision col) =>
        {
            Debug.Log("CollisionExit!");
        };
    }

    private void OnDestroy()
    {
        CollisionManager.GetInstance().UnregisterCollider(this);
        OnCollisionExit2D = null;
        OnCollisionEnter2D = null;
    }

    public void CalculateCollisions(IEnumerable<CustomCollider2D> colliders)
    {
        foreach (CustomCollider2D collider in colliders)
        {
            if (collider == this) continue;
            switch (ColliderType)
            {
                case ColliderType.Sphere:
                    {
                        switch (collider.ColliderType)
                        {
                            case ColliderType.Sphere:
                                {
                                    if (CircleCollision(this.transform.position + this.centerOffset, this.radius, collider.transform.position + collider.centerOffset, collider.radius))
                                    {
                                        if (AddCollider(collider))
                                            OnCollisionEnter2D(new CustomCollision(collider.transform.position + collider.centerOffset, collider));
                                    }
                                    else if (RemoveCollider(collider))
                                        OnCollisionExit2D(new CustomCollision(collider.transform.position + collider.centerOffset, collider));
                                }
                                break;
                            case ColliderType.AABB:
                                {
                                    if (CircleAABBCollision(this.transform.position + this.centerOffset, this.radius, collider.transform.position + collider.centerOffset, new Vector2(collider.width, collider.heigth)))
                                    {
                                        if (AddCollider(collider))
                                            OnCollisionEnter2D(new CustomCollision(GetCollisionPoint(this, collider), collider, GetCollisionNormal(this.PrevCenter, GetCollisionPoint(this, collider), collider)));
                                    }
                                    else if (RemoveCollider(collider))
                                        OnCollisionExit2D(new CustomCollision(GetCollisionPoint(this, collider), collider, GetCollisionNormal(this.PrevCenter, GetCollisionPoint(this, collider), collider)));
                                }
                                break;
                            default:
                                break;
                        }
                    }
                    break;
                case ColliderType.AABB:
                    {
                        switch (collider.ColliderType)
                        {
                            case ColliderType.Sphere:
                                {
                                    if (CircleAABBCollision(collider.transform.position + collider.centerOffset, collider.radius, this.transform.position + this.centerOffset, new Vector2(width, heigth)))
                                    {
                                        if (AddCollider(collider))
                                            OnCollisionEnter2D(new CustomCollision(GetCollisionPoint(collider, this), collider, GetCollisionNormal(collider.PrevCenter, GetCollisionPoint(collider, this), this)));
                                    }
                                    else if (RemoveCollider(collider))
                                        OnCollisionExit2D(new CustomCollision(GetCollisionPoint(collider, this), collider, GetCollisionNormal(collider.PrevCenter, GetCollisionPoint(collider, this), this)));
                                }
                                break;
                            case ColliderType.AABB:
                                {
                                    if (AABBCollision(transform.position + centerOffset, new Vector2(width, heigth), collider.transform.position + collider.centerOffset, new Vector2(collider.width, collider.heigth)))
                                    {
                                        if (AddCollider(collider))
                                            OnCollisionEnter2D(new CustomCollision(collider.transform.position + collider.centerOffset, collider));
                                    }
                                    else if (RemoveCollider(collider))
                                        OnCollisionExit2D(new CustomCollision(collider.transform.position + collider.centerOffset, collider));
                                }
                                break;
                            default:
                                break;
                        }
                    }
                    break;
                default:
                    break;
            }
        }
        prevPosition = Center;
    }

    private bool AddCollider(CustomCollider2D customCollider2D)
    {
        if (customCollider2DsCollissionning == null)
            customCollider2DsCollissionning = new List<CustomCollider2D>();

        if (customCollider2DsCollissionning.Contains(customCollider2D))
            return false;
        customCollider2DsCollissionning.Add(customCollider2D);
        return true;
    }

    private bool RemoveCollider(CustomCollider2D customCollider2D)
    {
        if (customCollider2DsCollissionning == null)
            customCollider2DsCollissionning = new List<CustomCollider2D>();

        if (!customCollider2DsCollissionning.Contains(customCollider2D))
            return false;
        customCollider2DsCollissionning.Remove(customCollider2D);
        return true;
    }

    private bool CircleCollision(Vector2 c1Pos, float c1Radius, Vector2 c2Pos, float c2Radius)
    {
        float distance = Mathf.Sqrt((c1Pos.x - c2Pos.x) * (c1Pos.x - c2Pos.x) + ((c1Pos.y - c2Pos.y) * (c1Pos.y - c2Pos.y)));
        return distance < c1Radius + c2Radius;
    }

    private bool AABBCollision(Vector2 s1Pos, Vector2 s1Size, Vector2 s2Pos, Vector2 s2Size)
    {
        s1Pos -= new Vector2(s1Size.x / 2, s1Size.y / 2);
        s2Pos -= new Vector2(s2Size.x / 2, s2Size.y / 2);

        return (s1Pos.x < s2Pos.x + s2Size.x &&
                s1Pos.x + s1Size.x > s2Pos.x &&
                s1Pos.y < s2Pos.y + s2Size.y &&
                s1Pos.y + s1Size.y > s2Pos.y);
    }

    private bool CircleAABBCollision(Vector2 circlePosition, float circleRadius, Vector2 aabbPosition, Vector2 aabbSize)
    {
        Vector2 dir = aabbPosition - circlePosition;
        dir = dir.normalized;
        dir *= circleRadius;
        Vector2 pos = circlePosition + dir;

        aabbPosition = aabbPosition - new Vector2(aabbSize.x / 2, aabbSize.y / 2);
        return (aabbPosition.x < pos.x &&
                aabbPosition.x + aabbSize.x > pos.x &&
                aabbPosition.y < pos.y &&
                aabbPosition.y + aabbSize.y > pos.y);
    }

    private Vector3 GetCollisionPoint(CustomCollider2D col1, CustomCollider2D col2)
    {
        Vector3 dirVector = (col2.transform.position + col2.centerOffset) - (col1.transform.position + col1.centerOffset);
        Vector3 closestPoint = dirVector.normalized * col1.radius;

        return col1.transform.position + closestPoint;
    }

    private Vector2 GetCollisionNormal(Vector2 spherePosition, Vector2 collisionPoint, CustomCollider2D col) {
        Vector2 normal = Vector2.zero;

        Vector2 max = col.Center + new Vector2(col.width / 2, col.heigth / 2);
        Vector2 min = col.Center + new Vector2(-col.width / 2, -col.heigth / 2);

        if (max.x > spherePosition.x && min.x < spherePosition.x)
            if (spherePosition.x > col.Center.x)
                normal = Vector2.up;
            else
                normal = Vector2.down;

        if (max.y > spherePosition.y && min.y < spherePosition.y)
            if (spherePosition.y > col.Center.y)
                normal = Vector2.left;
            else
                normal = Vector2.right;

        return normal == Vector2.zero? spherePosition - collisionPoint : normal;
    }
}