using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Drawing;
using Color = UnityEngine.Color;
using UnityEngine.Experimental.PlayerLoop;
using UnityEngine.UI;
using System.Security.Cryptography;
using System.Data;

namespace GRA_Project
{
    public class GRA : MonoBehaviour
    {
        public int Obstacle_Num = 0;
        public int Robot_Num = 0;
        public int Robot_Choose = 0;
        public int Search_Rotate_Angle = 5;
        public int Frame = 0;
        public int Step = -2;
        public int[][] Map_Value0;
        public int[][] Map_Value1;

        public Obstacle[] obstacle;
        public Robot[] robot;
        public Goal[] goal;

        public int display = 0;
        public int clear = 0;

        public bool Animation_Flag = false;
        public bool Path_Found;
        public bool[][][] Search;

        public GameObject Obj;

        public Texture2D Potential_Field0;
        public Texture2D Potential_Field1;

        int Obstacle_Chosen = 0;
        int Robot_Chosen = 0;
        string[] ObstacleStrings = { "Obstacle", "Obstacle0", "Obstacle1", "Obstacle2", "Obstacle3", "Obstacle4", "Obstacle5" };
        string[] RobotStrings = { "Robot", "Robot0", "Robot1", "Robot2", "Robot3", "Robot4", "Robot5" };

        public class Configuration
        {
            public float x;
            public float y;
            public float angle;

            public Configuration Parent_Configuration;           

            public void Set_Status(float Axis_x, float Axis_y, float Rotate_z, Configuration Parent)
            {
                x = Axis_x;
                y = Axis_y;
                angle = Rotate_z;
                Parent_Configuration = Parent;
            }
        }
        public class List_Configuration
        {
            public List<Configuration> figure = new List<Configuration>();
            
            public bool Empty()
            {
                return (figure.Count == 0);
            } 
            public void Push(Configuration con)
            {
                figure.Insert(0, con);
            }
            public void Pushback(Configuration con)
            {
                figure.Insert(figure.Count, con);
            }
            public void Pop()
            {
                figure.RemoveAt(0);
            }
            public Configuration Top()
            {
                return figure[0];
            }
        }

        public Configuration Initial_Configuration;
        public List_Configuration[] Path;
        public List<Configuration> Temp_Answer;
        public List<Configuration> Answer;

        public class Obstacle //  The information of obstacles
        {
            public int Polygon_Num;  // The number of polygons for each obstacle
            public int[] Polygon_Vertex_Num;   // The number of vertex for each polygon of the obstacle
            public Vertex[][] Polygon_Vertex_Coordinate;   // The coordinate of each vertex of polygon of obstacles
            public Position Initial;    // The initial position of each obstacle
            public GameObject[] Polygon;    // The Polygon Gameobject of obstacles
            public struct Vertex    // The coordinate of each vertex
            {
                public float x;
                public float y;
            }
            public struct Position  // The initial configuration of the obstacle
            {
                public float x;
                public float y;
                public float angle;
            }
        }
        public class Robot //  The information of robots
        {
            public int Polygon_Num = 0;    // The number of polygons for each robot
            public int Control_Point_Num = 0;  // The number of control point of each robot
            public int[] Polygon_Vertex_Num;    // The number of vertex for each polygon of the robots

            public Vertex[][] Polygon_Vertex_Coordinate;  // The coordinate of each vertex of polygon of robots
            public Vertex[] Control_Point;   // The position of each control point of robots

            public Position Initial; // The initial position of each robot
            public Position Goal; // The goal position of each robot

            public GameObject[] Polygon;    // The Polygon Gameobject of obstacles
            public struct Vertex    // The coordinate of each vertex
            {
                public float x;
                public float y;
            }
            public struct Position  // The initial configuration of the obstacle
            {
                public float x;
                public float y;
                public float angle;
            }
        }
        public class Goal
        {
            public GameObject[] Polygon;
        }

        void OnGUI()
        {
            
            Obstacle_Chosen = GUI.Toolbar(new Rect(0, 0, 500, 50), Obstacle_Chosen, ObstacleStrings);
            Robot_Chosen = GUI.Toolbar(new Rect(0, 50, 500, 50), Robot_Chosen, RobotStrings);
                        
            if (GUI.Button(new Rect(0, 100, 100, 50), "Display"))
            {
                if (display == clear)
                {
                    String Path_Obstacle = @"" + ObstacleStrings[Obstacle_Chosen] + ".dat";
                    String Path_Robot = @"" + RobotStrings[Robot_Chosen] + ".dat";

                    string[] Obstacle_lines;
                    Obstacle_lines = File.ReadAllLines(Path_Obstacle);

                    Input_Obstacle_Ver2(Obstacle_lines);

                    string[] Robot_lines;
                    Robot_lines = File.ReadAllLines(Path_Robot);

                    Input_Robot_Ver2(Robot_lines);

                    if (Robot_Num == 1)
                    {
                        Robot_Choose = 0;
                    }

                    Draw();
                    display++;

                }
            }
            if (GUI.Button(new Rect(0, 150, 100, 50), "Potential Field"))
            {
                Construct_Bitmap();
                
            }
            if (GUI.Button(new Rect(0, 200, 100, 50), "Find Path"))
            {
                bool Collide_Flag = Check_First();

                if (!Collide_Flag)
                {
                    BFS();
                }
                else
                {
                    print("Illegal Configuration!");
                }
            }
            if (GUI.Button(new Rect(0, 250, 100, 50), "Animation"))
            {
                Animation_Flag = !Animation_Flag;
            }
            if (GUI.Button(new Rect(0, 300, 100, 50), "Clear"))
            {

                for(int i = 0; i < Obstacle_Num; ++i)
                {
                    for(int j = 0; j < obstacle[i].Polygon_Num; ++j)
                    {
                        Destroy(obstacle[i].Polygon[j]);
                    }
                }

                for (int j = 0; j < robot[Robot_Choose].Polygon_Num; ++j)
                {
                    Destroy(robot[Robot_Choose].Polygon[j]);
                    Destroy(goal[Robot_Choose].Polygon[j]);
                }
                Frame = 0;
                Step = -2;
                clear++;
            }
            if (GUI.Button(new Rect(0, 350, 100, 50), "Robot0"))
            {
                Robot_Choose = 0;
            }
            if (GUI.Button(new Rect(0, 400, 100, 50), "Robot1"))
            {
                 Robot_Choose = 1;
            }
        }
        public void Draw()  // The function of drawing obstacles, robots and goals
        {
            for(int i = 0; i < Obstacle_Num; ++i)
            {
                GameObject Obstacle_tmp  = Instantiate(Obj);
                Obstacle_tmp.name = "Obstacle" + i.ToString();

                for (int j = 0; j < obstacle[i].Polygon_Num; ++j)
                {

                    obstacle[i].Polygon[j] = Instantiate(Obj);
                    obstacle[i].Polygon[j].transform.parent = Obstacle_tmp.transform;
                    obstacle[i].Polygon[j].transform.name = "Polygon" + j.ToString();

                    obstacle[i].Polygon[j].AddComponent<PolygonCollider2D>();
                    obstacle[i].Polygon[j].GetComponent<PolygonCollider2D>().isTrigger = true;
                    obstacle[i].Polygon[j].GetComponent<MeshFilter>().mesh = Obstacle_CreateMesh(i, j);
                    obstacle[i].Polygon[j].GetComponent<MeshRenderer>().material = Obstacle_CreateMaterial();
                    obstacle[i].Polygon[j].AddComponent<MouseEvent>();

                    if (j == obstacle[i].Polygon_Num - 1)
                    {
                        obstacle[i].Polygon[j].transform.parent.rotation = Quaternion.Euler(0, 0, obstacle[i].Initial.angle);
                        obstacle[i].Polygon[j].transform.parent.position = new Vector3(obstacle[i].Initial.x, obstacle[i].Initial.y, 0);
                    }
                }
            }

            GameObject Robot_tmp = Instantiate(Obj);
            Robot_tmp.name = "Robot" + Robot_Choose.ToString();

            for (int j = 0; j < robot[Robot_Choose].Polygon_Num; ++j)
            {
                robot[Robot_Choose].Polygon[j] = Instantiate(Obj);
                robot[Robot_Choose].Polygon[j].transform.parent = Robot_tmp.transform;
                robot[Robot_Choose].Polygon[j].transform.name = "Polygon" + j.ToString();

                robot[Robot_Choose].Polygon[j].AddComponent<PolygonCollider2D>();
                robot[Robot_Choose].Polygon[j].GetComponent<PolygonCollider2D>().isTrigger = true;
                robot[Robot_Choose].Polygon[j].GetComponent<MeshFilter>().mesh = Robot_CreateMesh(Robot_Choose, j);
                robot[Robot_Choose].Polygon[j].GetComponent<MeshRenderer>().material = Robot_CreateMaterial();
                robot[Robot_Choose].Polygon[j].AddComponent<MouseEvent>();

                if(j == robot[Robot_Choose].Polygon_Num - 1)
                {
                    robot[Robot_Choose].Polygon[j].transform.parent.rotation = Quaternion.Euler(0, 0, robot[Robot_Choose].Initial.angle);
                    robot[Robot_Choose].Polygon[j].transform.parent.position = new Vector3 (robot[Robot_Choose].Initial.x, robot[Robot_Choose].Initial.y, 0);                    
                }
            }        
            
            GameObject Goal_tmp = Instantiate(Obj);
            Goal_tmp.name = "Goal" + Robot_Choose.ToString();

            for (int j = 0; j < robot[Robot_Choose].Polygon_Num; ++j)
            {
                goal[Robot_Choose].Polygon[j] = Instantiate(Obj);
                goal[Robot_Choose].Polygon[j].transform.parent = Goal_tmp.transform;
                goal[Robot_Choose].Polygon[j].transform.name = "Polygon" + j.ToString();

                goal[Robot_Choose].Polygon[j].AddComponent<PolygonCollider2D>();
                goal[Robot_Choose].Polygon[j].GetComponent<PolygonCollider2D>().isTrigger = true;
                goal[Robot_Choose].Polygon[j].GetComponent<MeshFilter>().mesh = Goal_CreateMesh(Robot_Choose, j);
                goal[Robot_Choose].Polygon[j].GetComponent<MeshRenderer>().material = Goal_CreateMaterial();
                goal[Robot_Choose].Polygon[j].AddComponent<MouseEvent>();

                if (j == robot[Robot_Choose].Polygon_Num - 1)
                {
                    goal[Robot_Choose].Polygon[j].transform.parent.rotation = Quaternion.Euler(0, 0, robot[Robot_Choose].Goal.angle);
                    goal[Robot_Choose].Polygon[j].transform.parent.position = new Vector3(robot[Robot_Choose].Goal.x, robot[Robot_Choose].Goal.y, 0);
                }
            }
        }
        public Material Obstacle_CreateMaterial()   // Paint Obstacles
        {
            Material material = new Material(Shader.Find("Transparent/Diffuse"));
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Obstacle.png");
            material.mainTexture = texture;
            return material;
        }
        public Material Robot_CreateMaterial()  // Paint Robots
        {
            Material material = new Material(Shader.Find("Transparent/Diffuse"));
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Robot.png");
            material.mainTexture = texture;
            return material;
        }
        public Material Goal_CreateMaterial()   // Paint Goals
        {
            Material material = new Material(Shader.Find("Transparent/Diffuse"));
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Goal.png");
            material.mainTexture = texture;
            return material;
        }
        public Mesh Obstacle_CreateMesh(int i, int j)   // Draw Obstacles
        {
            Mesh mesh = new Mesh();

            Vector3[] vertices = new Vector3[obstacle[i].Polygon_Vertex_Num[j]];
            Vector2[] vert = new Vector2[obstacle[i].Polygon_Vertex_Num[j]];

            for (int k = 0; k < obstacle[i].Polygon_Vertex_Num[j]; ++k)
            {
                vertices[k] = new Vector3(obstacle[i].Polygon_Vertex_Coordinate[j][k].x, obstacle[i].Polygon_Vertex_Coordinate[j][k].y, 0);
                vert[k] = new Vector2(obstacle[i].Polygon_Vertex_Coordinate[j][k].x, obstacle[i].Polygon_Vertex_Coordinate[j][k].y);
            }

            obstacle[i].Polygon[j].GetComponent<PolygonCollider2D>().SetPath(obstacle[i].Polygon[j].GetComponent<PolygonCollider2D>().pathCount - 1, vert);

            mesh.vertices = vertices;

            int[] triangles = new int[(obstacle[i].Polygon_Vertex_Num[j] - 2) * 3];

            for (int k = 0; k < (obstacle[i].Polygon_Vertex_Num[j] - 2) * 3; ++k)
            {
                if (k % 3 == 0)
                {
                    triangles[k] = 0;
                }
                else if (k % 3 == 1)
                {
                    triangles[k] = k / 3 + 2;
                }
                else
                {
                    triangles[k] = k / 3 + 1;
                }
            }
            mesh.triangles = triangles;
            mesh.name = "triangle";
            return mesh;
        }
        public Mesh Robot_CreateMesh(int i, int j)  // Draw Robots and Goals
        {
            Mesh mesh = new Mesh();

            Vector3[] vertices = new Vector3[robot[i].Polygon_Vertex_Num[j]];
            Vector2[] vert = new Vector2[robot[i].Polygon_Vertex_Num[j]];

            for (int k = 0; k < robot[i].Polygon_Vertex_Num[j]; ++k)
            {
                vertices[k] = new Vector3(robot[i].Polygon_Vertex_Coordinate[j][k].x, robot[i].Polygon_Vertex_Coordinate[j][k].y, 0);
                vert[k] = new Vector2(robot[i].Polygon_Vertex_Coordinate[j][k].x, robot[i].Polygon_Vertex_Coordinate[j][k].y);
            }

            robot[i].Polygon[j].GetComponent<PolygonCollider2D>().SetPath(robot[i].Polygon[j].GetComponent<PolygonCollider2D>().pathCount - 1, vert);

            mesh.vertices = vertices;

            int[] triangles = new int[(robot[i].Polygon_Vertex_Num[j] - 2) * 3];

            for (int k = 0; k < (robot[i].Polygon_Vertex_Num[j] - 2) * 3; ++k)
            {
                if (k % 3 == 0)
                {
                    triangles[k] = 0;
                }
                else if (k % 3 == 1)
                {
                    triangles[k] = k / 3 + 2;
                }
                else
                {
                    triangles[k] = k / 3 + 1;
                }
            }
            mesh.triangles = triangles;
            mesh.name = "triangle";
            return mesh;
        }
        public Mesh Goal_CreateMesh(int i, int j)  // Draw Robots and Goals
        {
            Mesh mesh = new Mesh();

            Vector3[] vertices = new Vector3[robot[i].Polygon_Vertex_Num[j]];
            Vector2[] vert = new Vector2[robot[i].Polygon_Vertex_Num[j]];

            for (int k = 0; k < robot[i].Polygon_Vertex_Num[j]; ++k)
            {
                vertices[k] = new Vector3(robot[i].Polygon_Vertex_Coordinate[j][k].x, robot[i].Polygon_Vertex_Coordinate[j][k].y, 0);
                vert[k] = new Vector2(robot[i].Polygon_Vertex_Coordinate[j][k].x, robot[i].Polygon_Vertex_Coordinate[j][k].y);
            }

            goal[i].Polygon[j].GetComponent<PolygonCollider2D>().SetPath(goal[i].Polygon[j].GetComponent<PolygonCollider2D>().pathCount - 1, vert);

            mesh.vertices = vertices;

            int[] triangles = new int[(robot[i].Polygon_Vertex_Num[j] - 2) * 3];

            for (int k = 0; k < (robot[i].Polygon_Vertex_Num[j] - 2) * 3; ++k)
            {
                if (k % 3 == 0)
                {
                    triangles[k] = 0;
                }
                else if (k % 3 == 1)
                {
                    triangles[k] = k / 3 + 2;
                }
                else
                {
                    triangles[k] = k / 3 + 1;
                }
            }
            mesh.triangles = triangles;
            mesh.name = "triangle";
            return mesh;
        }
        public Obstacle.Vertex Obstacle_Current_Transform(Obstacle.Vertex ver, int i, int j)
        {
            Obstacle.Vertex temp_vertex;

            float Rotate_Angle = obstacle[i].Polygon[j].transform.parent.eulerAngles.z;
            float Trans_X = obstacle[i].Polygon[j].transform.parent.position.x;
            float Trans_Y = obstacle[i].Polygon[j].transform.parent.position.y;

            temp_vertex.x = ver.x * Mathf.Cos(Rotate_Angle * Mathf.Deg2Rad) - ver.y * Mathf.Sin(Rotate_Angle * Mathf.Deg2Rad);
            temp_vertex.y = ver.x * Mathf.Sin(Rotate_Angle * Mathf.Deg2Rad) + ver.y * Mathf.Cos(Rotate_Angle * Mathf.Deg2Rad);

            temp_vertex.x += Trans_X;
            temp_vertex.y += Trans_Y;

            return temp_vertex;
        }
        public Robot.Vertex Robot_Current_Transform(Robot.Vertex ver)
        {
            Robot.Vertex temp_vertex = new Robot.Vertex();

            float Rotate_Angle = robot[Robot_Choose].Polygon[0].transform.parent.eulerAngles.z;
            float Trans_X = robot[Robot_Choose].Polygon[0].transform.parent.position.x;
            float Trans_Y = robot[Robot_Choose].Polygon[0].transform.parent.position.y;

            temp_vertex.x = ver.x * Mathf.Cos(Rotate_Angle * Mathf.Deg2Rad) - ver.y * Mathf.Sin(Rotate_Angle * Mathf.Deg2Rad);
            temp_vertex.y = ver.x * Mathf.Sin(Rotate_Angle * Mathf.Deg2Rad) + ver.y * Mathf.Cos(Rotate_Angle * Mathf.Deg2Rad);

            temp_vertex.x += Trans_X;
            temp_vertex.y += Trans_Y;

            return temp_vertex;
        }
        public Robot.Vertex Current_Position(Robot.Vertex v, Configuration con)
        {
            Robot.Vertex temp_vertex = new Robot.Vertex();

            float Rotate_Angle = con.angle;
            float Trans_X = con.x;
            float Trans_Y = con.y;

            temp_vertex.x = v.x * Mathf.Cos(Rotate_Angle * Mathf.Deg2Rad) - v.y * Mathf.Sin(Rotate_Angle * Mathf.Deg2Rad);
            temp_vertex.y = v.x * Mathf.Sin(Rotate_Angle * Mathf.Deg2Rad) + v.y * Mathf.Cos(Rotate_Angle * Mathf.Deg2Rad);

            temp_vertex.x += Trans_X;
            temp_vertex.y += Trans_Y;

            return temp_vertex;
        }

        public Obstacle.Vertex Current_Position(Obstacle.Vertex v, Configuration con)
        {
            Obstacle.Vertex temp_vertex = new Obstacle.Vertex();

            float Rotate_Angle = con.angle;
            float Trans_X = con.x;
            float Trans_Y = con.y;

            temp_vertex.x = v.x * Mathf.Cos(Rotate_Angle * Mathf.Deg2Rad) - v.y * Mathf.Sin(Rotate_Angle * Mathf.Deg2Rad);
            temp_vertex.y = v.x * Mathf.Sin(Rotate_Angle * Mathf.Deg2Rad) + v.y * Mathf.Cos(Rotate_Angle * Mathf.Deg2Rad);

            temp_vertex.x += Trans_X;
            temp_vertex.y += Trans_Y;

            return temp_vertex;
        }

        public bool Inside_Polygon(Obstacle.Vertex[] Vertices, Vector2 point, int i, int j)
        {
            Obstacle.Vertex[] New_Vertex = new Obstacle.Vertex[obstacle[i].Polygon_Vertex_Num[j]];
            New_Vertex[0] = Obstacle_Current_Transform(Vertices[0], i, j);
            float Min_X = New_Vertex[0].x;
            float Min_Y = New_Vertex[0].y;
            float Max_X = New_Vertex[0].x;
            float Max_Y = New_Vertex[0].y;

            for(int k = 1; k < Vertices.Length; ++k)
            {
                New_Vertex[k] = Obstacle_Current_Transform(Vertices[k], i, j);

                Min_X = Mathf.Min(Min_X, New_Vertex[k].x);
                Max_X = Mathf.Max(Max_X, New_Vertex[k].x);
                Min_Y = Mathf.Min(Min_Y, New_Vertex[k].y);
                Max_Y = Mathf.Max(Max_Y, New_Vertex[k].y);
            }

            if(point.x < Min_X || point.x > Max_X || point.y < Min_Y || point.y > Max_Y)
            {
                return false;
            }

            bool inside = false;
            for (int m = 0, n = Vertices.Length - 1; m < Vertices.Length; n = m, ++m)
            {
                if(point.y < New_Vertex[n].y)
                {
                    if(New_Vertex[m].y <= point.y)
                    {
                        if((point.y - New_Vertex[m].y) * (New_Vertex[n].x - New_Vertex[m].x) > (point.x - New_Vertex[m].x) * (New_Vertex[n].y - New_Vertex[m].y))
                        {
                            inside = !inside;
                        }
                    }
                }
                else if(point.y < New_Vertex[m].y)
                {
                    if((point.y - New_Vertex[m].y) * (New_Vertex[n].x - New_Vertex[m].x) < (point.x - New_Vertex[m].x) * (New_Vertex[n].y - New_Vertex[m].y))
                    {
                        inside = !inside;
                    }
                }
            }
            return inside;
            
        }

        public void Construct_Bitmap()
        {

            int[][] BitMap0 = new int[128][];
            int[][] BitMap1 = new int[128][];
            for (int i = 0; i < 128; ++i)
            {
                BitMap0[i] = new int[128];
                BitMap1[i] = new int[128];
            }
            for (int i = 0; i < 128; ++i)
            {
                for (int j = 0; j < 128; ++j)
                {
                    BitMap0[i][j] = 254;
                    BitMap1[i][j] = 254;
                }
            }

            for (int x = 0; x < 128; ++x)
            {
                for (int y = 0; y < 128; ++y)
                {
                    bool inside = false;
                    for (int i = 0; i < Obstacle_Num; ++i)
                    {
                        for (int j = 0; j < obstacle[i].Polygon_Num; ++j)
                        {
                            if (Inside_Polygon(obstacle[i].Polygon_Vertex_Coordinate[j], new Vector2(x, y), i, j))
                            {
                                BitMap0[x][y] = 255;
                                BitMap1[x][y] = 255;
                                inside = true;
                                break;
                            }
                        }
                        if (inside)
                        {
                            break;
                        }
                    }
                }
            }
            Robot.Vertex[] Control0 = new Robot.Vertex[Robot_Num]; 
            Robot.Vertex[] Control1 = new Robot.Vertex[Robot_Num];
            Robot.Vertex[] New_Control0 = new Robot.Vertex[Robot_Num];
            Robot.Vertex[] New_Control1 = new Robot.Vertex[Robot_Num];


            Control0[Robot_Choose] = robot[Robot_Choose].Control_Point[0];
            Control1[Robot_Choose] = robot[Robot_Choose].Control_Point[1];

            New_Control0[Robot_Choose].x = Control0[Robot_Choose].x * Mathf.Cos(Mathf.Deg2Rad * goal[Robot_Choose].Polygon[0].transform.parent.eulerAngles.z) - Control0[Robot_Choose].y * Mathf.Sin(Mathf.Deg2Rad * goal[Robot_Choose].Polygon[0].transform.parent.eulerAngles.z) + goal[Robot_Choose].Polygon[0].transform.parent.position.x;
            New_Control0[Robot_Choose].y = Control0[Robot_Choose].x * Mathf.Sin(Mathf.Deg2Rad * goal[Robot_Choose].Polygon[0].transform.parent.eulerAngles.z) + Control0[Robot_Choose].y * Mathf.Cos(Mathf.Deg2Rad * goal[Robot_Choose].Polygon[0].transform.parent.eulerAngles.z) + goal[Robot_Choose].Polygon[0].transform.parent.position.y;
            New_Control1[Robot_Choose].x = Control1[Robot_Choose].x * Mathf.Cos(Mathf.Deg2Rad * goal[Robot_Choose].Polygon[0].transform.parent.eulerAngles.z) - Control1[Robot_Choose].y * Mathf.Sin(Mathf.Deg2Rad * goal[Robot_Choose].Polygon[0].transform.parent.eulerAngles.z) + goal[Robot_Choose].Polygon[0].transform.parent.position.x;
            New_Control1[Robot_Choose].y = Control1[Robot_Choose].x * Mathf.Sin(Mathf.Deg2Rad * goal[Robot_Choose].Polygon[0].transform.parent.eulerAngles.z) + Control1[Robot_Choose].y * Mathf.Cos(Mathf.Deg2Rad * goal[Robot_Choose].Polygon[0].transform.parent.eulerAngles.z) + goal[Robot_Choose].Polygon[0].transform.parent.position.y;


            BitMap0[(int) New_Control0[Robot_Choose].x][(int) New_Control0[Robot_Choose].y] = 0;
            BitMap1[(int) New_Control1[Robot_Choose].x][(int) New_Control1[Robot_Choose].y] = 0;

            Point Goal0 = new Point((int)New_Control0[Robot_Choose].x, (int)New_Control0[Robot_Choose].y);
            Point Goal1 = new Point((int)New_Control1[Robot_Choose].x, (int)New_Control1[Robot_Choose].y); ;

            Build_Potential_Field0(BitMap0, Goal0);
            Build_Potential_Field1(BitMap1, Goal1);
            
        }
        public bool In_Box(float x, float y)
        {
            if(x < 128 && x >= 0 && y < 128 && y >= 0)
            {
                return true;
            }
            return false;
        }
        public void Build_Potential_Field0(int[][] Map, Point point)
        {
            Queue<Point> queue = new Queue<Point>();
            queue.Enqueue(point);

            while(queue.Count > 0)
            {
                if(In_Box(queue.Peek().X - 1, queue.Peek().Y) && Map[queue.Peek().X - 1][queue.Peek().Y] == 254)
                {
                    Map[queue.Peek().X - 1][queue.Peek().Y] = Map[queue.Peek().X][queue.Peek().Y] + 1;
                    queue.Enqueue(new Point(queue.Peek().X - 1, queue.Peek().Y));
                }
                if (In_Box(queue.Peek().X + 1, queue.Peek().Y) && Map[queue.Peek().X + 1][queue.Peek().Y] == 254)
                {
                    Map[queue.Peek().X + 1][queue.Peek().Y] = Map[queue.Peek().X][queue.Peek().Y] + 1;
                    queue.Enqueue(new Point(queue.Peek().X + 1, queue.Peek().Y));
                }
                if (In_Box(queue.Peek().X, queue.Peek().Y - 1) && Map[queue.Peek().X][queue.Peek().Y - 1] == 254)
                {
                    Map[queue.Peek().X][queue.Peek().Y - 1] = Map[queue.Peek().X][queue.Peek().Y] + 1;
                    queue.Enqueue(new Point(queue.Peek().X, queue.Peek().Y - 1));
                }
                if (In_Box(queue.Peek().X, queue.Peek().Y + 1) && Map[queue.Peek().X][queue.Peek().Y + 1] == 254)
                {
                    Map[queue.Peek().X][queue.Peek().Y + 1] = Map[queue.Peek().X][queue.Peek().Y] + 1;
                    queue.Enqueue(new Point(queue.Peek().X, queue.Peek().Y + 1));
                }
                queue.Dequeue();
            }
            
            Color[][] color = new Color[128][];

            Map_Value0 = new int[128][];

            for (int i = 0; i < 128; ++i)
            {
                color[i] = new Color[128];
                Map_Value0[i] = new int[128];
            }
            for (int y = 0; y < 128; ++y)
            {
                for (int x = 0; x < 128; ++x)
                {
                    color[x][y] = new Color(Map[x][y] / 255f, Map[x][y] / 255f, Map[x][y] / 255f);
                    Potential_Field0.SetPixel(x, y, color[x][y]);

                    Map_Value0[x][y] = Map[x][y];
                }
            }       
            Potential_Field0.Apply();
        }
        public void Build_Potential_Field1(int[][] Map, Point point)
        {
            Queue<Point> queue = new Queue<Point>();
            queue.Enqueue(point);

            while (queue.Count > 0)
            {
                if (In_Box(queue.Peek().X - 1, queue.Peek().Y) && Map[queue.Peek().X - 1][queue.Peek().Y] == 254)
                {
                    Map[queue.Peek().X - 1][queue.Peek().Y] = Map[queue.Peek().X][queue.Peek().Y] + 1;
                    queue.Enqueue(new Point(queue.Peek().X - 1, queue.Peek().Y));
                }
                if (In_Box(queue.Peek().X + 1, queue.Peek().Y) && Map[queue.Peek().X + 1][queue.Peek().Y] == 254)
                {
                    Map[queue.Peek().X + 1][queue.Peek().Y] = Map[queue.Peek().X][queue.Peek().Y] + 1;
                    queue.Enqueue(new Point(queue.Peek().X + 1, queue.Peek().Y));
                }
                if (In_Box(queue.Peek().X, queue.Peek().Y - 1) && Map[queue.Peek().X][queue.Peek().Y - 1] == 254)
                {
                    Map[queue.Peek().X][queue.Peek().Y - 1] = Map[queue.Peek().X][queue.Peek().Y] + 1;
                    queue.Enqueue(new Point(queue.Peek().X, queue.Peek().Y - 1));
                }
                if (In_Box(queue.Peek().X, queue.Peek().Y + 1) && Map[queue.Peek().X][queue.Peek().Y + 1] == 254)
                {
                    Map[queue.Peek().X][queue.Peek().Y + 1] = Map[queue.Peek().X][queue.Peek().Y] + 1;
                    queue.Enqueue(new Point(queue.Peek().X, queue.Peek().Y + 1));
                }
                queue.Dequeue();
            }

            Color[][] color = new Color[128][];
        
            Map_Value1 = new int[128][];

            for (int i = 0; i < 128; ++i)
            {
                color[i] = new Color[128];
                Map_Value1[i] = new int[128];
            }
            for (int y = 0; y < 128; ++y)
            {
                for (int x = 0; x < 128; ++x)
                {
                    color[x][y] = new Color(Map[x][y] / 255f, Map[x][y] / 255f, Map[x][y] / 255f);
                    Potential_Field1.SetPixel(x, y, color[x][y]);

                    Map_Value1[x][y] = Map[x][y];
                }
            }
            Potential_Field1.Apply();
        }
        public bool Check_First()
        {
            Obstacle.Vertex Obstacle_Vertex1 = new Obstacle.Vertex();
            Obstacle.Vertex Obstacle_Vertex2 = new Obstacle.Vertex();
            
            Robot.Vertex Robot_Vertex1 = new Robot.Vertex();
            Robot.Vertex Robot_Vertex2 = new Robot.Vertex();

            Robot.Vertex Goal_Vertex1 = new Robot.Vertex();
            Robot.Vertex Goal_Vertex2 = new Robot.Vertex();

            Configuration Obstacle_Vertex_Configuration = new Configuration();
            Configuration Robot_Vertex_Configuration = new Configuration();
            Configuration Goal_Vertex_Configuration = new Configuration();

            for (int i = 0; i < Obstacle_Num; ++i)
            {
                for(int j = 0; j < obstacle[i].Polygon_Num; ++j)
                {
                    for(int k = 0; k < obstacle[i].Polygon_Vertex_Num[j]; ++k)
                    {
                        for(int m = 0; m < robot[Robot_Choose].Polygon_Num; ++m)
                        {
                            for(int n = 0; n < robot[Robot_Choose].Polygon_Vertex_Num[m]; ++n)
                            {
                                Obstacle_Vertex_Configuration.x = obstacle[i].Polygon[j].transform.parent.position.x;
                                Obstacle_Vertex_Configuration.y = obstacle[i].Polygon[j].transform.parent.position.y;
                                Obstacle_Vertex_Configuration.angle = obstacle[i].Polygon[j].transform.parent.eulerAngles.z;
                                Obstacle_Vertex1 = Current_Position(obstacle[i].Polygon_Vertex_Coordinate[j][k], Obstacle_Vertex_Configuration);

                                Robot_Vertex_Configuration.x = robot[Robot_Choose].Polygon[m].transform.parent.position.x;
                                Robot_Vertex_Configuration.y = robot[Robot_Choose].Polygon[m].transform.parent.position.y;
                                Robot_Vertex_Configuration.angle = robot[Robot_Choose].Polygon[m].transform.parent.eulerAngles.z;
                                Robot_Vertex1 = Current_Position(robot[Robot_Choose].Polygon_Vertex_Coordinate[m][n], Robot_Vertex_Configuration);

                                Goal_Vertex_Configuration.x = goal[Robot_Choose].Polygon[m].transform.parent.position.x;
                                Goal_Vertex_Configuration.y = goal[Robot_Choose].Polygon[m].transform.parent.position.y;
                                Goal_Vertex_Configuration.angle = goal[Robot_Choose].Polygon[m].transform.parent.eulerAngles.z;
                                Goal_Vertex1 = Current_Position(robot[Robot_Choose].Polygon_Vertex_Coordinate[m][n], Goal_Vertex_Configuration);

                                Obstacle_Vertex2 = Current_Position(obstacle[i].Polygon_Vertex_Coordinate[j][(k + 1) % obstacle[i].Polygon_Vertex_Num[j]], Obstacle_Vertex_Configuration);
                                Robot_Vertex2 = Current_Position(robot[Robot_Choose].Polygon_Vertex_Coordinate[m][(n + 1) % robot[Robot_Choose].Polygon_Vertex_Num[m]], Robot_Vertex_Configuration);
                                Goal_Vertex2 = Current_Position(robot[Robot_Choose].Polygon_Vertex_Coordinate[m][(n + 1) % robot[Robot_Choose].Polygon_Vertex_Num[m]], Goal_Vertex_Configuration);

                                if(Collision_Happened(Obstacle_Vertex1, Obstacle_Vertex2, Robot_Vertex1, Robot_Vertex2))
                                {
                                    return true;
                                }
                                if(Collision_Happened(Obstacle_Vertex1, Obstacle_Vertex2, Goal_Vertex1, Goal_Vertex2))
                                {
                                    return true;
                                }
                                if(!In_Box(Obstacle_Vertex1.x, Obstacle_Vertex1.y))
                                {
                                    return true;
                                }
                                if (!In_Box(Obstacle_Vertex2.x, Obstacle_Vertex2.y))
                                {
                                    return true;
                                }
                                if (!In_Box(Robot_Vertex1.x, Robot_Vertex1.y))
                                {
                                    return true;
                                }
                                if (!In_Box(Robot_Vertex2.x, Robot_Vertex2.y))
                                {
                                    return true;
                                }
                                if (!In_Box(Goal_Vertex1.x, Goal_Vertex1.y))
                                {
                                    return true;
                                }
                                if (!In_Box(Goal_Vertex2.x, Goal_Vertex2.y))
                                {
                                    return true;
                                }

                            }
                        }
                    }
                }
            }
            return false;
        }
        public bool Moving_Collide(Configuration Robot_Current_Configuration)
        {

            Configuration Obstacle_Vertex_Configuration = new Configuration();

            for (int i = 0; i < Obstacle_Num; ++i)
            {
                for (int j = 0; j < obstacle[i].Polygon_Num; ++j)
                {
                    Obstacle_Vertex_Configuration.x = obstacle[i].Polygon[j].transform.parent.position.x;
                    Obstacle_Vertex_Configuration.y = obstacle[i].Polygon[j].transform.parent.position.y;
                    Obstacle_Vertex_Configuration.angle = obstacle[i].Polygon[j].transform.parent.eulerAngles.z;

                    for (int k = 0; k < obstacle[i].Polygon_Vertex_Num[j]; ++k)
                    {
                        Obstacle.Vertex Obstacle_Vertex1 = Current_Position(obstacle[i].Polygon_Vertex_Coordinate[j][k], Obstacle_Vertex_Configuration);
                        Obstacle.Vertex Obstacle_Vertex2 = Current_Position(obstacle[i].Polygon_Vertex_Coordinate[j][(k + 1) % obstacle[i].Polygon_Vertex_Num[j]], Obstacle_Vertex_Configuration);

                        for (int m = 0; m < robot[Robot_Choose].Polygon_Num; ++m)
                        {
                            for (int n = 0; n < robot[Robot_Choose].Polygon_Vertex_Num[m]; ++n)
                            {
                                Robot.Vertex Robot_Vertex1 = Current_Position(robot[Robot_Choose].Polygon_Vertex_Coordinate[m][n], Robot_Current_Configuration);
                                Robot.Vertex Robot_Vertex2 = Current_Position(robot[Robot_Choose].Polygon_Vertex_Coordinate[m][(n + 1) % robot[Robot_Choose].Polygon_Vertex_Num[m]], Robot_Current_Configuration);

                                if (Collision_Happened(Obstacle_Vertex1, Obstacle_Vertex2, Robot_Vertex1, Robot_Vertex2))
                                {
                                    return true;
                                }

                            }
                        }
                    }
                }
            }
            return false;
        }
        public bool Moving_Outside(Configuration Robot_Current_Configuration)
        {

            for (int m = 0; m < robot[Robot_Choose].Polygon_Num; ++m)
            {
                for (int n = 0; n < robot[Robot_Choose].Polygon_Vertex_Num[m]; ++n)
                {
                    Robot.Vertex Robot_Vertex1 = Current_Position(robot[Robot_Choose].Polygon_Vertex_Coordinate[m][n], Robot_Current_Configuration);

                    if (!In_Box(Robot_Vertex1.x, Robot_Vertex1.y))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public bool Collision_Happened(Obstacle.Vertex p1, Obstacle.Vertex p2, Robot.Vertex p3, Robot.Vertex p4)
        {
            Vector2 v12 = new Vector2();
            Vector2 v13 = new Vector2();
            Vector2 v14 = new Vector2();
            Vector2 v31 = new Vector2();
            Vector2 v32 = new Vector2();
            Vector2 v34 = new Vector2();

            v12.x = p1.y - p2.y;
            v12.y = p2.x - p1.x;
            v13.x = p3.x - p1.x;
            v13.y = p3.y - p1.y;
            v14.x = p4.x - p1.x;
            v14.y = p4.y - p1.y;

            v31.x = p1.x - p3.x;
            v31.y = p1.y - p3.y;
            v32.x = p2.x - p3.x;
            v32.y = p2.y - p3.y;
            v34.x = p3.y - p4.y;
            v34.y = p4.x - p3.x;

            if((Vector2.Dot(v12, v13) * Vector2.Dot(v12, v14) < 0) && (Vector2.Dot(v34, v31) * Vector2.Dot(v34, v32) < 0))
            {
                return true;
            }
            return false;
        }        
        public int Energy_Sum(Robot.Vertex v1, Robot.Vertex v2)
        {
            return Map_Value0[(int) v1.x][(int) v1.y] + Map_Value1[(int) v2.x][(int) v2.y];
        }
        public bool All_Path_Empty()
        {
            for(int i = 0; i < Path.Length; ++i)
            {
                if (!Path[i].Empty())
                {
                    return false;
                }
            }
            return true;
        }
        public Configuration Smallest_Energy()
        {
            Configuration Temp_Configuration = new Configuration();

            for (int i = 0; i < Path.Length; ++i)
            {
                if (!Path[i].Empty())
                {
                    Temp_Configuration.x = Path[i].Top().x;
                    Temp_Configuration.y = Path[i].Top().y;
                    Temp_Configuration.angle = Path[i].Top().angle;

                    if (i != 0)
                    {
                        Path[i].Pop();
                    }
                    else
                    {
                        Path_Found = true;                       
                    }
                    break;
                }
            }
            return Temp_Configuration;
        }
        public void BFS()
        {
            Path = new List_Configuration[511];
            Temp_Answer = new List<Configuration>();
            Answer = new List<Configuration>();

            for(int i = 0; i < 511; ++i)
            {
                Path[i] = new List_Configuration();
            }

            Search = new bool[128][][];
            for (int i = 0; i < 128; ++i)
            {
                Search[i] = new bool[128][];
                for (int j = 0; j < 128; ++j)
                {
                    Search[i][j] = new bool[360 / Search_Rotate_Angle];
                    for (int k = 0; k < 360 / Search_Rotate_Angle; ++k)
                    {
                        Search[i][j][k] = false;
                    }
                }
            }

            Robot.Vertex Temp_Control0 = new Robot.Vertex();
            Robot.Vertex Temp_Control1 = new Robot.Vertex();

            Temp_Control0 = robot[Robot_Choose].Control_Point[0];
            Temp_Control1 = robot[Robot_Choose].Control_Point[1];

            Robot.Vertex Initial_Control0 = Robot_Current_Transform(Temp_Control0);
            Robot.Vertex Initial_Control1 = Robot_Current_Transform(Temp_Control1);

            Configuration First_Configuration = new Configuration();

            First_Configuration.Set_Status(robot[Robot_Choose].Polygon[0].transform.parent.position.x, robot[Robot_Choose].Polygon[0].transform.parent.position.y, robot[Robot_Choose].Polygon[0].transform.parent.eulerAngles.z, null);
            Search[(int) robot[Robot_Choose].Polygon[0].transform.parent.position.x][(int) robot[Robot_Choose].Polygon[0].transform.parent.position.y][(int) robot[Robot_Choose].Polygon[0].transform.parent.eulerAngles.z / Search_Rotate_Angle] = true;

            Path[Energy_Sum(Initial_Control0, Initial_Control1)].Push(First_Configuration);

            Temp_Answer.Add(First_Configuration);

            Configuration parent_configuration = new Configuration();

            int[] Delta_X = new int[6] {1, 0, -1, 0, 0, 0};
            int[] Delta_Y = new int[6] {0, 1, 0, -1, 0, 0};
            int[] Delta_Rotation = new int[6] {0, 0, 0, 0, Search_Rotate_Angle, -Search_Rotate_Angle };

            Path_Found = false;

            while(!Path_Found && !All_Path_Empty())
            {
                parent_configuration = Smallest_Energy();                

                for (int i = 0; i < 6; ++i)
                {
                    Configuration Next_Configuration = new Configuration();
                    Next_Configuration.Set_Status(parent_configuration.x + Delta_X[i], parent_configuration.y + Delta_Y[i], parent_configuration.angle + Delta_Rotation[i], parent_configuration);

                    if(In_Box(Next_Configuration.x, Next_Configuration.y))
                    {
                        if(Next_Configuration.angle >= 360)
                        {
                            Next_Configuration.angle -= 360;
                        } 
                        else if(Next_Configuration.angle < 0)
                        {
                            Next_Configuration.angle += 360;
                        }

                        if(!Search[(int) Next_Configuration.x][(int) Next_Configuration.y][(int) Next_Configuration.angle / Search_Rotate_Angle])
                        {
                            Search[(int)Next_Configuration.x][(int)Next_Configuration.y][(int)Next_Configuration.angle / Search_Rotate_Angle] = true;

                            if (!Moving_Outside(Next_Configuration) && !Moving_Collide(Next_Configuration))
                            {
                                Robot.Vertex New_Control0 = Current_Position(robot[Robot_Choose].Control_Point[0], Next_Configuration);
                                Robot.Vertex New_Control1 = Current_Position(robot[Robot_Choose].Control_Point[1], Next_Configuration);

                                Path[Energy_Sum(New_Control0, New_Control1)].Push(Next_Configuration);
                                Temp_Answer.Add(Next_Configuration);
                            }
                        }
                    }
                }
                
            }
            if (Path_Found)
            {
                print("Oh My God! Excellent!!! Ha Ha! I Found The Path XDDD");

                Answer.Add(Temp_Answer[Temp_Answer.Count - 1]);

                int index = Temp_Answer.Count - 1;

                while (Temp_Answer[index].Parent_Configuration != null) {

                    Answer.Add(Temp_Answer[index].Parent_Configuration);

                    for (int i = 0; i < Temp_Answer.Count; ++i)
                    {
                        if (Temp_Answer[i].x == Temp_Answer[index].Parent_Configuration.x && Temp_Answer[i].y == Temp_Answer[index].Parent_Configuration.y && Temp_Answer[i].angle == Temp_Answer[index].Parent_Configuration.angle)
                        {
                            index = i;
                            break;
                        }
                    }
                }

            }
            else
            {
                print("Oh No! So Sad QAQQQQ I Can't Find The Path. Where Is The Path???");
            }
        }

        public void Input_Obstacle(string[] Data)
        {
            string Obstacle_ID = null;  // The ID of the obstacle
            string Polygon_ID = null;   // The ID of the polygon

            for (int i = 0; i < Data.Length; ++i)
            {
                if (Data[i][0] == '#')   // If the first character is "#"
                {
                    if (Data[i][2] == 'n' && Data[i][12] == 'o')    // If the statement is "# number of obstacles"
                    {
                        Obstacle_Num = int.Parse(Data[i + 1]);
                        obstacle = new Obstacle[Obstacle_Num];

                    }
                    else if (Data[i][2] == 'o')   // If the statement is "# obstacle #"
                    {
                        Obstacle_ID = null;  // Reset the ID of obstacle

                        for (int j = 12; j < Data[i].Length; ++j)
                        {
                            Obstacle_ID += Data[i][j];
                        }

                        obstacle[int.Parse(Obstacle_ID)] = new Obstacle();
                        obstacle[int.Parse(Obstacle_ID)].Polygon_Num = new int();
                        obstacle[int.Parse(Obstacle_ID)].Polygon_Num = int.Parse(Data[i + 2]); // Set the number of obstacles' polygon
                        obstacle[int.Parse(Obstacle_ID)].Polygon = new GameObject[int.Parse(Data[i + 2])];
                        obstacle[int.Parse(Obstacle_ID)].Polygon_Vertex_Num = new int[int.Parse(Data[i + 2])];
                        obstacle[int.Parse(Obstacle_ID)].Polygon_Vertex_Coordinate = new Obstacle.Vertex[obstacle[int.Parse(Obstacle_ID)].Polygon_Num][];

                    }
                    else if (Data[i][2] == 'p') // If the statement is "# polygon #"
                    {
                        Polygon_ID = null;   // Reset the ID of polygon

                        for (int j = 11; j < Data[i].Length; ++j)
                        {
                            Polygon_ID += Data[i][j];
                        }
                        
                        obstacle[int.Parse(Obstacle_ID)].Polygon_Vertex_Num[int.Parse(Polygon_ID)] = int.Parse(Data[i + 2]);    // Set the number of vertex of the polygon of obstacle                       
                        obstacle[int.Parse(Obstacle_ID)].Polygon_Vertex_Coordinate[int.Parse(Polygon_ID)] = new Obstacle.Vertex[obstacle[int.Parse(Obstacle_ID)].Polygon_Vertex_Num[int.Parse(Polygon_ID)]];

                    }
                    else if (Data[i][2] == 'v') // If the statement is "# vertices"
                    {

                        for (int j = i + 1; j <= i + int.Parse(Data[i - 1]); ++j)
                        {
                            string[] XY = Data[j].Split(' ');

                            float.TryParse(XY[0], out float X);
                            float.TryParse(XY[1], out float Y);

                            //  Set the coordinate of each vertex of the polygon
                            
                            obstacle[int.Parse(Obstacle_ID)].Polygon_Vertex_Coordinate[int.Parse(Polygon_ID)][j - (i + 1)].x = X;
                            obstacle[int.Parse(Obstacle_ID)].Polygon_Vertex_Coordinate[int.Parse(Polygon_ID)][j - (i + 1)].y = Y;

                        }
                    }
                    else if (Data[i][2] == 'i')  // If the statement is "# initial configuration"
                    {
                        string[] initial = Data[i + 1].Split(' ');

                        float.TryParse(initial[0], out float X);
                        float.TryParse(initial[1], out float Y);
                        float.TryParse(initial[2], out float Angle);

                        //  Set the initial configuration of each obstacle
                        obstacle[int.Parse(Obstacle_ID)].Initial = new Obstacle.Position();
                        obstacle[int.Parse(Obstacle_ID)].Initial.x = X;
                        obstacle[int.Parse(Obstacle_ID)].Initial.y = Y;
                        obstacle[int.Parse(Obstacle_ID)].Initial.angle = Angle;

                    }
                }
            }
        }
        public void Input_Robot(string[] Data)
        {

            string Robot_ID = null;
            string Polygon_ID = null;

            for (int i = 0; i < Data.Length; ++i)
            {
                if (Data[i][0] == '#')   // If the first character is "#"
                {
                    if (Data[i][2] == 'n' && Data[i][12] == 'r')    // If the statement is "# number of robots"
                    {
                        Robot_Num = int.Parse(Data[i + 1]);
                        robot = new Robot[Robot_Num];
                        goal = new Goal[Robot_Num];
                    }
                    else if (Data[i][2] == 'r')   // If the statement is "# robot #"
                    {
                        Robot_ID = null;  // Reset the ID of robot

                        for (int j = 9; j < Data[i].Length; ++j)
                        {
                            Robot_ID += Data[i][j];
                        }

                        robot[int.Parse(Robot_ID)] = new Robot();
                        robot[int.Parse(Robot_ID)].Polygon_Num = new int();
                        robot[int.Parse(Robot_ID)].Polygon_Num = int.Parse(Data[i + 2]);   // Set the number of robots' polygon
                        robot[int.Parse(Robot_ID)].Polygon = new GameObject[int.Parse(Data[i + 2])];
                        robot[int.Parse(Robot_ID)].Polygon_Vertex_Num = new int[int.Parse(Data[i + 2])];
                        robot[int.Parse(Robot_ID)].Polygon_Vertex_Coordinate = new Robot.Vertex[robot[int.Parse(Robot_ID)].Polygon_Num][];

                        goal[int.Parse(Robot_ID)] = new Goal();
                        goal[int.Parse(Robot_ID)].Polygon = new GameObject[int.Parse(Data[i + 2])];

                    }
                    else if (Data[i][2] == 'p') // If the statement is "# polygon #"
                    {
                        Polygon_ID = null;   // Reset the ID of polygon

                        for (int j = 11; j < Data[i].Length; ++j)
                        {
                            Polygon_ID += Data[i][j];
                        }

                        robot[int.Parse(Robot_ID)].Polygon_Vertex_Num[int.Parse(Polygon_ID)] = int.Parse(Data[i + 2]);    // Set the number of vertex of the polygon of robot
                        robot[int.Parse(Robot_ID)].Polygon_Vertex_Coordinate[int.Parse(Polygon_ID)] = new Robot.Vertex[robot[int.Parse(Robot_ID)].Polygon_Vertex_Num[int.Parse(Polygon_ID)]];
                    }
                    else if (Data[i][2] == 'v') // If the statement is "# vertices"
                    {

                        for (int j = i + 1; j <= i + int.Parse(Data[i - 1]); ++j)
                        {
                            string[] XY = Data[j].Split(' ');

                            float.TryParse(XY[0], out float X);
                            float.TryParse(XY[1], out float Y);

                            //  Set the coordinate of each vertex of the polygon
                            
                            robot[int.Parse(Robot_ID)].Polygon_Vertex_Coordinate[int.Parse(Polygon_ID)][j - (i + 1)].x = X;
                            robot[int.Parse(Robot_ID)].Polygon_Vertex_Coordinate[int.Parse(Polygon_ID)][j - (i + 1)].y = Y;
                        }
                    }
                    else if (Data[i][2] == 'i')  // If the statement is "# initial configuration"
                    {
                        string[] initial = Data[i + 1].Split(' ');

                        float.TryParse(initial[0], out float X);
                        float.TryParse(initial[1], out float Y);
                        float.TryParse(initial[2], out float Angle);

                        //  Set the initial configuration of each robot
                        robot[int.Parse(Robot_ID)].Initial = new Robot.Position();
                        robot[int.Parse(Robot_ID)].Initial.x = X;
                        robot[int.Parse(Robot_ID)].Initial.y = Y;
                        robot[int.Parse(Robot_ID)].Initial.angle = Angle;
                    }
                    else if (Data[i][2] == 'g') // If the statement is "# goal configuration"
                    {
                        string[] goal = Data[i + 1].Split(' ');

                        float.TryParse(goal[0], out float X);
                        float.TryParse(goal[1], out float Y);
                        float.TryParse(goal[2], out float Angle);

                        //  Set the goal configuration of each robot
                        robot[int.Parse(Robot_ID)].Goal = new Robot.Position();
                        robot[int.Parse(Robot_ID)].Goal.x = X;
                        robot[int.Parse(Robot_ID)].Goal.y = Y;
                        robot[int.Parse(Robot_ID)].Goal.angle = Angle;
                    }
                    else if (Data[i][2] == 'n' && Data[i][12] == 'c')   // If the statement is "# number of control points"
                    {
                        robot[int.Parse(Robot_ID)].Control_Point_Num = new int();
                        robot[int.Parse(Robot_ID)].Control_Point_Num = int.Parse(Data[i + 1]);
                        robot[int.Parse(Robot_ID)].Control_Point = new Robot.Vertex[robot[int.Parse(Robot_ID)].Control_Point_Num];
                    }
                    else if (Data[i][2] == 'c') // If the statement is "# control point #"
                    {
                        string Control_Point_ID = null;    // Reset the ID of control point

                        for (int j = 17; j < Data[i].Length; ++j)
                        {
                            Control_Point_ID += Data[i][j];
                        }

                        string[] XY = Data[i + 1].Split(' ');

                        float.TryParse(XY[0], out float X);
                        float.TryParse(XY[1], out float Y);

                        // Set the Position of each control point of robots 
                        robot[int.Parse(Robot_ID)].Control_Point[int.Parse(Control_Point_ID)].x = X;
                        robot[int.Parse(Robot_ID)].Control_Point[int.Parse(Control_Point_ID)].y = Y;
                    }
                }
            }
        }

        public void Input_Obstacle_Ver2(string[] Data)
        {
            for (int i = 0; i < Data.Length; ++i)
            {
                
                if (Data[i][0] != '#' && Data[i][0] != ' ')   // If the first character is "#"
                {
                    Obstacle_Num = int.Parse(Data[i]);
                    obstacle = new Obstacle[Obstacle_Num];

                    for(int Obstacle_ID = 0; Obstacle_ID < Obstacle_Num; ++Obstacle_ID) 
                    {
                        for(int j = i + 1; j < Data.Length; ++j)
                        {
                            if (Data[j][0] != '#' && Data[j][0] != ' ')
                            {
                                obstacle[Obstacle_ID] = new Obstacle();
                                obstacle[Obstacle_ID].Polygon_Num = new int();
                                obstacle[Obstacle_ID].Polygon_Num = int.Parse(Data[j]); // Set the number of obstacles' polygon
                                obstacle[Obstacle_ID].Polygon = new GameObject[int.Parse(Data[j])];
                                obstacle[Obstacle_ID].Polygon_Vertex_Num = new int[int.Parse(Data[j])];
                                obstacle[Obstacle_ID].Polygon_Vertex_Coordinate = new Obstacle.Vertex[obstacle[Obstacle_ID].Polygon_Num][];

                                for (int Polygon_ID = 0; Polygon_ID < obstacle[Obstacle_ID].Polygon_Num; ++Polygon_ID)
                                {
                                    for (int k = j + 1; k < Data.Length; ++k)
                                    {
                                        if (Data[k][0] != '#' && Data[k][0] != ' ')
                                        {
                                            obstacle[Obstacle_ID].Polygon_Vertex_Num[Polygon_ID] = int.Parse(Data[k]);    // Set the number of vertex of the polygon of obstacle                       
                                            obstacle[Obstacle_ID].Polygon_Vertex_Coordinate[Polygon_ID] = new Obstacle.Vertex[obstacle[Obstacle_ID].Polygon_Vertex_Num[Polygon_ID]];

                                            for (int Vertex_ID = 0; Vertex_ID < obstacle[Obstacle_ID].Polygon_Vertex_Num[Polygon_ID]; ++Vertex_ID)
                                            {
                                                for (int l = k + 1; l < Data.Length; ++l)
                                                {
                                                    if (Data[l][0] != '#' && Data[l][0] != ' ')
                                                    {
                                                        string[] XY = Data[l].Split(' ');

                                                        float.TryParse(XY[0], out float X);
                                                        float.TryParse(XY[1], out float Y);

                                                        //  Set the coordinate of each vertex of the polygon

                                                        obstacle[Obstacle_ID].Polygon_Vertex_Coordinate[Polygon_ID][Vertex_ID].x = X;
                                                        obstacle[Obstacle_ID].Polygon_Vertex_Coordinate[Polygon_ID][Vertex_ID].y = Y;

                                                        ++Vertex_ID;

                                                        if (Vertex_ID == obstacle[Obstacle_ID].Polygon_Vertex_Num[Polygon_ID])
                                                        {
                                                            k = l;
                                                            break;
                                                        }
                                                    }
                                                }
                                            }
                                            ++Polygon_ID;

                                            if (Polygon_ID == obstacle[Obstacle_ID].Polygon_Num)
                                            {
                                                j = k;
                                                break;
                                            }
                                        }
                                    }
                                    if (Polygon_ID == obstacle[Obstacle_ID].Polygon_Num)
                                    {
                                        for (j = j + 1; j < Data.Length; ++j)
                                        {
                                            if (Data[j][0] != '#' && Data[j][0] != ' ')
                                            {
                                                string[] initial = Data[j].Split(' ');

                                                float.TryParse(initial[0], out float X);
                                                float.TryParse(initial[1], out float Y);
                                                float.TryParse(initial[2], out float Angle);

                                                //  Set the initial configuration of each obstacle
                                                obstacle[Obstacle_ID].Initial = new Obstacle.Position();
                                                obstacle[Obstacle_ID].Initial.x = X;
                                                obstacle[Obstacle_ID].Initial.y = Y;
                                                obstacle[Obstacle_ID].Initial.angle = Angle;

                                                break;
                                            }
                                        }
                                    }
                                }
                                ++Obstacle_ID;

                                if (Obstacle_ID == Obstacle_Num)
                                {
                                    i = j;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
        public void Input_Robot_Ver2(string[] Data)
        {
            for (int i = 0; i < Data.Length; ++i)
            {
                if (Data[i][0] != '#' && Data[i][0] != ' ')   // If the first character is "#"
                {
                    Robot_Num = int.Parse(Data[i]);
                    robot = new Robot[Robot_Num];
                    goal = new Goal[Robot_Num];

                    for (int Robot_ID = 0; Robot_ID < Robot_Num; ++Robot_ID)
                    {
                        for (int j = i + 1; j < Data.Length; ++j)
                        {
                            if (Data[j][0] != '#' && Data[j][0] != ' ')
                            {
                                robot[Robot_ID] = new Robot();
                                robot[Robot_ID].Polygon_Num = new int();
                                robot[Robot_ID].Polygon_Num = int.Parse(Data[j]); // Set the number of obstacles' polygon
                                robot[Robot_ID].Polygon = new GameObject[int.Parse(Data[j])];
                                robot[Robot_ID].Polygon_Vertex_Num = new int[int.Parse(Data[j])];
                                robot[Robot_ID].Polygon_Vertex_Coordinate = new Robot.Vertex[robot[Robot_ID].Polygon_Num][];

                                goal[Robot_ID] = new Goal();
                                goal[Robot_ID].Polygon = new GameObject[int.Parse(Data[j])];

                                for (int Polygon_ID = 0; Polygon_ID < robot[Robot_ID].Polygon_Num; ++Polygon_ID)
                                {
                                    for (int k = j + 1; k < Data.Length; ++k)
                                    {
                                        if (Data[k][0] != '#' && Data[k][0] != ' ')
                                        {
                                            //Debug.Log(k);
                                            robot[Robot_ID].Polygon_Vertex_Num[Polygon_ID] = int.Parse(Data[k]);    // Set the number of vertex of the polygon of obstacle                       
                                            robot[Robot_ID].Polygon_Vertex_Coordinate[Polygon_ID] = new Robot.Vertex[robot[Robot_ID].Polygon_Vertex_Num[Polygon_ID]];

                                            for (int Vertex_ID = 0; Vertex_ID < robot[Robot_ID].Polygon_Vertex_Num[Polygon_ID]; ++Vertex_ID)
                                            {
                                                for (int l = k + 1; l < Data.Length; ++l)
                                                {
                                                    if (Data[l][0] != '#' && Data[l][0] != ' ')
                                                    {
                                                        string[] XY = Data[l].Split(' ');

                                                        float.TryParse(XY[0], out float X);
                                                        float.TryParse(XY[1], out float Y);

                                                        //  Set the coordinate of each vertex of the polygon

                                                        robot[Robot_ID].Polygon_Vertex_Coordinate[Polygon_ID][Vertex_ID].x = X;
                                                        robot[Robot_ID].Polygon_Vertex_Coordinate[Polygon_ID][Vertex_ID].y = Y;

                                                        ++Vertex_ID;

                                                        if (Vertex_ID == robot[Robot_ID].Polygon_Vertex_Num[Polygon_ID])
                                                        {
                                                            //print(l);
                                                            k = l;
                                                            break;
                                                        }
                                                    }
                                                }
                                            }
                                            ++Polygon_ID;

                                            if (Polygon_ID == robot[Robot_ID].Polygon_Num)
                                            {
                                                j = k;
                                                break;
                                            }
                                        }
                                    }
                                    if (Polygon_ID == robot[Robot_ID].Polygon_Num)
                                    {
                                        for (j = j + 1; j < Data.Length; ++j)
                                        {
                                            if (Data[j][0] != '#' && Data[j][0] != ' ')
                                            {
                                                string[] initial = Data[j].Split(' ');

                                                float.TryParse(initial[0], out float X);
                                                float.TryParse(initial[1], out float Y);
                                                float.TryParse(initial[2], out float Angle);

                                                //  Set the initial configuration of each obstacle
                                                robot[Robot_ID].Initial = new Robot.Position();
                                                robot[Robot_ID].Initial.x = X;
                                                robot[Robot_ID].Initial.y = Y;
                                                robot[Robot_ID].Initial.angle = Angle;

                                                break;
                                            }
                                        }

                                        for (j = j + 1; j < Data.Length; ++j)
                                        {
                                            if (Data[j][0] != '#' && Data[j][0] != ' ')
                                            {
                                                string[] goal = Data[j].Split(' ');

                                                float.TryParse(goal[0], out float X);
                                                float.TryParse(goal[1], out float Y);
                                                float.TryParse(goal[2], out float Angle);

                                                //  Set the goal configuration of each robot
                                                robot[Robot_ID].Goal = new Robot.Position();
                                                robot[Robot_ID].Goal.x = X;
                                                robot[Robot_ID].Goal.y = Y;
                                                robot[Robot_ID].Goal.angle = Angle;

                                                break;
                                            }
                                        }
                                        for (j = j + 1; j < Data.Length; ++j)
                                        {
                                            if (Data[j][0] != '#' && Data[j][0] != ' ')
                                            {
                                                robot[Robot_ID].Control_Point_Num = new int();
                                                robot[Robot_ID].Control_Point_Num = int.Parse(Data[j]);
                                                robot[Robot_ID].Control_Point = new Robot.Vertex[robot[Robot_ID].Control_Point_Num];

                                                break;
                                            }
                                        }
                                        for (j = j + 1; j < Data.Length; ++j)
                                        {
                                            if (Data[j][0] != '#' && Data[j][0] != ' ')
                                            {
                                                string[] XY = Data[j].Split(' ');

                                                float.TryParse(XY[0], out float X);
                                                float.TryParse(XY[1], out float Y);

                                                // Set the Position of each control point of robots 
                                                robot[Robot_ID].Control_Point[0].x = X;
                                                robot[Robot_ID].Control_Point[0].y = Y;

                                                break;
                                            }
                                        }
                                        for (j = j + 1; j < Data.Length; ++j)
                                        {
                                            if (Data[j][0] != '#' && Data[j][0] != ' ')
                                            {
                                                string[] XY = Data[j].Split(' ');

                                                float.TryParse(XY[0], out float X);
                                                float.TryParse(XY[1], out float Y);

                                                // Set the Position of each control point of robots 
                                                robot[Robot_ID].Control_Point[1].x = X;
                                                robot[Robot_ID].Control_Point[1].y = Y;

                                                break;
                                            }
                                        }
                                    }
                                }
                                ++Robot_ID;

                                if (Robot_ID == Robot_Num)
                                {
                                    i = j;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        void Start()
        {
            
        }
        // Update is called once per frame
        void Update()
        {

            if (Animation_Flag)
            {
                Frame++;
                
                if (Step == -2)
                {
                    Step = Answer.Count - 1;
                }

                if(Frame % 2 == 0 && Step > 0)
                {

                    robot[Robot_Choose].Polygon[0].transform.parent.position = new Vector3(Answer[Step].x, Answer[Step].y, 0);
                    robot[Robot_Choose].Polygon[0].transform.parent.eulerAngles = new Vector3(0, 0, Answer[Step].angle);
                    --Step;

                    if(Step == 0) 
                    {
                        robot[Robot_Choose].Polygon[0].transform.parent.position = goal[Robot_Choose].Polygon[0].transform.parent.position;
                        robot[Robot_Choose].Polygon[0].transform.parent.eulerAngles = goal[Robot_Choose].Polygon[0].transform.parent.eulerAngles;
                        Animation_Flag = false;
                    }
                }
            }
        }
    }
}
