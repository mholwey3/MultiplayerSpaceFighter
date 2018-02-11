using UnityEngine;
using System.Collections;

public class MapLoader : MonoBehaviour
{
	private const string SPAWN = "21B24A";
	private const string WALL = "FFFFFF";

	public Texture2D map;
    public GameObject player;
    public GameObject wall;

	void Awake() {
		LoadMap();
	}

    void Start() {
        
    }

    private void LoadMap() {
        for(int y = 0; y < map.height; y++) {
            for(int x = 0; x < map.width; x++) {
                string pixColor = ColorUtility.ToHtmlStringRGB(map.GetPixel(x, y));
                float posX = x * wall.transform.localScale.x;
                float posY = y * wall.transform.localScale.y;
                Vector3 pos = new Vector3(posX, posY, 0f);
                if (pixColor.Equals(SPAWN)) {
                    Instantiate(player, pos, Quaternion.identity);
                } else if (pixColor.Equals(WALL)) {
                    Instantiate(wall, pos, Quaternion.identity);
                }
            }
        }
    }
}
