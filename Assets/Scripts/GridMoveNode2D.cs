using UnityEngine;

public class GridMoveNode2D : MonoBehaviour
{
    [Header("Neighbors")]
    [SerializeField] private GridMoveNode2D up;
    [SerializeField] private GridMoveNode2D down;
    [SerializeField] private GridMoveNode2D left;
    [SerializeField] private GridMoveNode2D right;

    public Vector2 Position => transform.position;

    public GridMoveNode2D GetNeighbor(Vector2Int direction)
    {
        if (direction == Vector2Int.up) return up;
        if (direction == Vector2Int.down) return down;
        if (direction == Vector2Int.left) return left;
        if (direction == Vector2Int.right) return right;
        return null;
    }
}
