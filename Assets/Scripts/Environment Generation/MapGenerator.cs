﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class MapGenerator : MonoBehaviour {

	public int width;
	public int height;

	public string seed;
	public bool useRandomSeed;

	[Range(0, 100)]
	public int randomFillPercent;

	public int smoothIterations;
	public int birthLimit;
	public int deathLimit;

    public float squareSize;

    [Range(1, 5)]
    public int passagewayRadius;

	int[,] map;

	void Awake () {
		GenerateMap();
	}

	void Update() {
		if (Input.GetMouseButtonDown(0)) {
			GenerateMap();
		}
	}

	/// <summary>
	/// Procedurally generates a map using cellular automata. Cleans up the map of regions that are too small.
	/// Connects rooms. Creates a border around the map. Generates the mesh of the resultant map.
	/// </summary>
	void GenerateMap() {
		map = new int[width, height];
		RandomFillMap();

		for(int i = 0; i < smoothIterations; i++) {
			SmoothMap();
		}

		RemoveSmallRegionsAndConnectRooms();

		int borderSize = 1;
		int[,] borderedMap = new int[width + borderSize * 2, height + borderSize * 2];

		for (int x = 0; x < borderedMap.GetLength(0); x++) {
			for (int y = 0; y < borderedMap.GetLength(1); y++) {
				if(x >= borderSize && x < width + borderSize && y >= borderSize && y < height + borderSize) {
					borderedMap[x, y] = map[x - borderSize, y - borderSize];
				} else {
					borderedMap[x, y] = 1;
				}
			}
		}

		MeshGenerator meshGen = GetComponent<MeshGenerator>();
		meshGen.GenerateMesh(borderedMap, squareSize);

		Grid mapGrid = GameObject.FindGameObjectWithTag("A_Star").GetComponent<Grid>();
		mapGrid.InitGrid(borderedMap, squareSize);
	}

	/// <summary>
	/// Removes wall regions and room regions that are smaller than the threshold size and connects the remaining rooms.
	/// </summary>
	void RemoveSmallRegionsAndConnectRooms() {
		List<List<Coord>> wallRegions = GetRegions(1);
		int wallThresholdSize = 50;
		foreach(List<Coord> wallRegion in wallRegions) {
			if(wallRegion.Count < wallThresholdSize) {
				foreach(Coord tile in wallRegion) {
					map[tile.tileX, tile.tileY] = 0;
				}
			}
		}

		List<List<Coord>> roomRegions = GetRegions(0);
		int roomThresholdSize = 50;
		List<Room> remainingRooms = new List<Room>();
		foreach (List<Coord> roomRegion in roomRegions) {
			if (roomRegion.Count < roomThresholdSize) {
				foreach (Coord tile in roomRegion) {
					map[tile.tileX, tile.tileY] = 1;
				}
			}else {
				remainingRooms.Add(new Room(roomRegion, map));
			}
		}

		remainingRooms.Sort();
		remainingRooms[0].isMainRoom = true;
		remainingRooms[0].isAccessibleFromMainRoom = true;
		ConnectClosestRooms(remainingRooms);
	}

	/// <summary>
	/// Finds the closest paths between all rooms and connects them with passages.
	/// </summary>
	/// <param name="allRooms"></param>
	/// <param name="forceAccessibilityFromMainRoom"></param>
	void ConnectClosestRooms(List<Room> allRooms, bool forceAccessibilityFromMainRoom = false) {
		List<Room> roomListA = new List<Room>(); // rooms that are NOT accessible from main room
		List<Room> roomListB = new List<Room>(); // rooms that are accessible from main room

		if (forceAccessibilityFromMainRoom) {
			foreach(Room room in allRooms) {
				if (room.isAccessibleFromMainRoom) {
					roomListB.Add(room);
				} else {
					roomListA.Add(room);
				}
			}
		} else {
			roomListA = allRooms;
			roomListB = allRooms;
		}

		int bestDistance = 0;
		Coord bestTileA = new Coord();
		Coord bestTileB = new Coord();
		Room bestRoomA = new Room();
		Room bestRoomB = new Room();
		bool possibleConnectionFound = false;

		foreach(Room roomA in roomListA) {

			// first pass through this method
			if (!forceAccessibilityFromMainRoom) {
				possibleConnectionFound = false;
				if(roomA.connectedRooms.Count > 0) {
					continue;
				}
			}

			foreach(Room roomB in roomListB) {
				if(roomA == roomB || roomA.IsConnected(roomB)) {
					continue;
				}

				for(int tileIndexA = 0; tileIndexA < roomA.edgeTiles.Count; tileIndexA++) {
					for (int tileIndexB = 0; tileIndexB < roomB.edgeTiles.Count; tileIndexB++) {
						Coord tileA = roomA.edgeTiles[tileIndexA];
						Coord tileB = roomB.edgeTiles[tileIndexB];
						int distanceBetweenRooms = (int)(Mathf.Pow(tileA.tileX - tileB.tileX, 2) + Mathf.Pow(tileA.tileY - tileB.tileY, 2));

						if(distanceBetweenRooms < bestDistance || !possibleConnectionFound) {
							bestDistance = distanceBetweenRooms;
							possibleConnectionFound = true;
							bestTileA = tileA;
							bestTileB = tileB;
							bestRoomA = roomA;
							bestRoomB = roomB;
						}
					}
				}
			}

			// Ensures that we only immediately create a passage between bestRoomA and bestRoomB
			// after iterating through all roomBs when executing the first pass of this method
			if (possibleConnectionFound && !forceAccessibilityFromMainRoom) {
				CreatePassage(bestRoomA, bestRoomB, bestTileA, bestTileB);
			}
		}

		// After going through second pass of this method, creates a passage from a room that is
		// accessible from the main room to the closest room from roomListA. Then call this method 
		// again to ensure connectivity
		if (possibleConnectionFound && forceAccessibilityFromMainRoom) {
			CreatePassage(bestRoomA, bestRoomB, bestTileA, bestTileB);
			ConnectClosestRooms(allRooms, true);
		}

		// AFter going through the first pass of this method, calls it again to ensure
		// that all rooms are accessible from the main room
		if (!forceAccessibilityFromMainRoom) {
			ConnectClosestRooms(allRooms, true);
		}
	}

	/// <summary>
	/// Creates a passage between two rooms using two Coords of these rooms.
	/// </summary>
	/// <param name="roomA"></param>
	/// <param name="roomB"></param>
	/// <param name="tileA"></param>
	/// <param name="tileB"></param>
	void CreatePassage(Room roomA, Room roomB, Coord tileA, Coord tileB) {
		Room.ConnectRooms(roomA, roomB);
        List<Coord> line = GetLine(tileA, tileB);
        foreach(Coord c in line) {
            DrawCircle(c, passagewayRadius);
        }
	}

	/// <summary>
	/// Makes Coord c and all Coords withing radius r traversable.
	/// </summary>
	/// <param name="c"></param>
	/// <param name="r"></param>
    void DrawCircle(Coord c, int r) {
        for(int x = -r; x <= r; x++) {
            for (int y = -r; y <= r; y++) {
                if (x * x + y * y <= r * r) {
                    int drawX = c.tileX + x;
                    int drawY = c.tileY + y;
                    if(IsInMapRange(drawX, drawY)) {
                        map[drawX, drawY] = 0;
                    }
                }
            }
        }
    }

	/// <summary>
	/// Gets a line between the two Coords.
	/// </summary>
	/// <param name="from"></param>
	/// <param name="to"></param>
	/// <returns>A list of Coords - the Coords that make up the line between from and to</returns>
    List<Coord> GetLine(Coord from, Coord to) {
        List<Coord> line = new List<Coord>();

        int x = from.tileX;
        int y = from.tileY;

        int dx = to.tileX - from.tileX;
        int dy = to.tileY - from.tileY;

        bool inverted = false;
        int step = Math.Sign(dx);
        int gradientStep = Math.Sign(dy);

        int longest = Mathf.Abs(dx);
        int shortest = Mathf.Abs(dy);

        if(longest < shortest) {
            inverted = true;
            longest = Mathf.Abs(dy);
            shortest = Mathf.Abs(dx);

            step = Math.Sign(dy);
            gradientStep = Math.Sign(dx);
        }

        int gradientAccumulation = longest / 2;
        for(int i = 0; i < longest; i++) {
            line.Add(new Coord(x, y));

            if (inverted) {
                y += step;
            } else {
                x += step;
            }

            gradientAccumulation += shortest;
            if(gradientAccumulation >= longest) {
                if (inverted) {
                    x += gradientStep;
                } else {
                    y += gradientStep;
                }
                gradientAccumulation -= longest;
            }
        }

        return line;
    }

	Vector3 CoordToWorldPoint(Coord tile) {
		return new Vector3(-width / 2 + .5f + tile.tileX, 20, -height / 2 + .5f + tile.tileY);
	}

	/// <summary>
	/// Gets either the rooms (0) or the walled-off (1) regions of the map, depending on the tileType.
	/// </summary>
	/// <param name="tileType">Signifies if the regions we're looking for are rooms (0) or walls (1)</param>
	/// <returns>A list of a list of Coords - the desired regions</returns>
	List<List<Coord>> GetRegions(int tileType) {
		List<List<Coord>> regions = new List<List<Coord>>();
		int[,] mapFlags = new int[width, height];

		for (int x = 0; x < width; x++) {
			for (int y = 0; y < height; y++) {
				if(mapFlags[x,y] == 0 && map[x,y] == tileType) {
					List<Coord> newRegion = GetRegionTiles(x, y);
					regions.Add(newRegion);

					foreach(Coord tile in newRegion) {
						mapFlags[tile.tileX, tile.tileY] = 1;
					}
				}
			}
		}

		return regions;
	}

	/// <summary>
	/// Gets all of the tiles contained within a region.
	/// </summary>
	/// <param name="startX"></param>
	/// <param name="startY"></param>
	/// <returns>A list of Coords - the region tiles</returns>
	List<Coord> GetRegionTiles(int startX, int startY) {
		List<Coord> tiles = new List<Coord>();
		int[,] mapFlags = new int[width, height];
		int tileType = map[startX, startY];

		Queue<Coord> queue = new Queue<Coord>();
		queue.Enqueue(new Coord(startX, startY));
		mapFlags[startX, startY] = 1;

		while (queue.Count > 0) {
			Coord tile = queue.Dequeue();
			tiles.Add(tile);

			for (int x = tile.tileX - 1; x <= tile.tileX + 1; x++) {
				for (int y = tile.tileY - 1; y <= tile.tileY + 1; y++) {
					if(IsInMapRange(x,y) && (y == tile.tileY || x == tile.tileX)) {
						if(mapFlags[x,y] == 0 && map[x,y] == tileType) {
							mapFlags[x, y] = 1;
							queue.Enqueue(new Coord(x, y));
						}
					}
				}
			}
		}

		return tiles;
	}

	/// <summary>
	/// Checks if the (x, y) coordinates are contained within the map.
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <returns>true if the coordinates are contained within the map; false otherwise</returns>
	bool IsInMapRange(int x, int y) {
		return x >= 0 && x < width && y >= 0 && y < height;
	}

	/// <summary>
	/// Randomly assigns the tiles of the map as on (1) or off (0).
	/// </summary>
	void RandomFillMap() {
		if (useRandomSeed) {
			seed = Time.time.ToString();
		}
		
		System.Random rng = new System.Random(seed.GetHashCode());

		for(int x = 0; x < width; x++) {
			for(int y = 0; y < height; y++) {
				if(x == 0 || x == width - 1 || y == 0 || y == height - 1) {
					map[x, y] = 1;
				} else {
					map[x, y] = (rng.Next(0, 100) < randomFillPercent) ? 1 : 0;
				}
			}
		}
	}

	/// <summary>
	/// Simulates Conway's Game of Life (or Cellular Automata depending on how fancy you want to sound)
	/// by assigning the map's tiles as on if there are enough neighboring tiles or off if there are not
	/// enough neighboring tiles.
	/// </summary>
	void SmoothMap() {
		for (int x = 0; x < width; x++) {
			for (int y = 0; y < height; y++) {
				int neighborWallTiles = GetSurroundingWallCount(x, y);

				if(neighborWallTiles > birthLimit) {
					map[x, y] = 1;
				} else if(neighborWallTiles < deathLimit) {
					map[x, y] = 0;
				}
			}
		}
	}

	/// <summary>
	/// For the given (x, y) tile on the map, gets the number of surrounding tiles that are walls.
	/// </summary>
	/// <param name="gridX"></param>
	/// <param name="gridY"></param>
	/// <returns></returns>
	int GetSurroundingWallCount(int gridX, int gridY) {
		int wallCount = 0;
		for(int neighborX = gridX - 1; neighborX <= gridX + 1; neighborX++) {
			for (int neighborY = gridY - 1; neighborY <= gridY + 1; neighborY++) {
				if (IsInMapRange(neighborX, neighborY)) {
					if (neighborX != gridX || neighborY != gridY) {
						wallCount += map[neighborX, neighborY];
					}
				} else {
					wallCount++;
				}
			}
		}

		return wallCount;
	}

	struct Coord {
		public int tileX;
		public int tileY;

		public Coord(int x, int y) {
			tileX = x;
			tileY = y;
		}
	}

	class Room : IComparable<Room>{
		public List<Coord> tiles;
		public List<Coord> edgeTiles;
		public List<Room> connectedRooms;
		public int roomSize; //# of tiles
		public bool isAccessibleFromMainRoom;
		public bool isMainRoom;

		public Room() {

		}

		public Room(List<Coord> roomTiles, int[,] map) {
			tiles = roomTiles;
			roomSize = tiles.Count;
			connectedRooms = new List<Room>();

			edgeTiles = new List<Coord>();
			foreach(Coord tile in tiles) {
				for(int x = tile.tileX-1; x <= tile.tileX+1; x++) {
					for (int y = tile.tileY - 1; y <= tile.tileY + 1; y++) {
						if(x == tile.tileX || y == tile.tileY) {
							if(map[x,y] == 1) {
								edgeTiles.Add(tile);
							}
						}
					}
				}
			}
		}

		public void SetAccessibleFromMainRoom() {
			if (!isAccessibleFromMainRoom) {
				isAccessibleFromMainRoom = true;
				foreach(Room connectedRoom in connectedRooms) {
					connectedRoom.SetAccessibleFromMainRoom();
				}
			}
		}

		// We start with the main room as the base. If we connect any room to the main room, 
		// this sets the isAccessibleFromMainRoom bool to true for that room, then any room that
		// gets connected to that room will be accessible from the main room.
		public static void ConnectRooms(Room roomA, Room roomB) {
			if (roomA.isAccessibleFromMainRoom) {
				roomB.SetAccessibleFromMainRoom();
			} else if (roomB.isAccessibleFromMainRoom) {
				roomA.SetAccessibleFromMainRoom();
			}
			roomA.connectedRooms.Add(roomB);
			roomB.connectedRooms.Add(roomA);
		}

		public bool IsConnected(Room otherRoom) {
			return connectedRooms.Contains(otherRoom);
		}

		public int CompareTo(Room otherRoom) {
			return otherRoom.roomSize.CompareTo(roomSize);
		}
	}
}
