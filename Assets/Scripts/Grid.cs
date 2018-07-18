using UnityEngine;
using System.Collections;

public class Grid : MonoBehaviour {

	public Transform player;

	private Vector2 gridWorldSize;
	private float nodeRadius, nodeDiameter;
	private Node[,] grid;
	private int gridSizeX, gridSizeY;

	public void InitGrid(int[,] map, float _nodeDiameter) {
		gridSizeX = map.GetLength(0);
		gridSizeY = map.GetLength(1);
		nodeDiameter = _nodeDiameter;
		nodeRadius = _nodeDiameter / 2;
		gridWorldSize = new Vector2(gridSizeX * nodeDiameter, gridSizeY * nodeDiameter);

		grid = new Node[gridSizeX, gridSizeY];
		Vector3 worldBottomLeft = transform.position - Vector3.right * gridWorldSize.x / 2 - Vector3.up * gridWorldSize.y / 2;

		for (int x = 0; x < gridSizeX; x++) {
			for (int y = 0; y < gridSizeY; y++) {
				Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * nodeDiameter + nodeRadius) + Vector3.up * (y * nodeDiameter + nodeRadius);
				bool walkable = (map[x, y] == 1) ? false : true;
				grid[x, y] = new Node(walkable, worldPoint);
			}
		}
	}

	public Node NodeFromWorldPoint(Vector3 worldPosition) {
		float percentX = (worldPosition.x + gridWorldSize.x / 2) / gridWorldSize.x;
		float percentY = (worldPosition.y + gridWorldSize.y / 2) / gridWorldSize.y;
		percentX = Mathf.Clamp01(percentX);
		percentY = Mathf.Clamp01(percentY);

		int x = Mathf.RoundToInt((gridSizeX - 1) * percentX);
		int y = Mathf.RoundToInt((gridSizeY - 1) * percentY);
		return grid[x, y];
	}

	void OnDrawGizmos() {
		Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, gridWorldSize.y, 1));

		if(grid != null) {
			Node playerNode = NodeFromWorldPoint(player.position);
			foreach(Node n in grid) {
				Gizmos.color = (n.walkable) ? Color.white : Color.red;
				if(playerNode == n) {
					Gizmos.color = Color.cyan;
				}
				Gizmos.DrawCube(n.worldPosition, Vector3.one * (nodeDiameter - .1f));
			}
		}
	}
}
