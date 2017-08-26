using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AiMovementNodeManager : MonoBehaviour
{
    public enum EConnectionType
    {
        Walking,
    }

    public class SNodeConnection
    {
        public AiMovementNode first;
        public AiMovementNode second;
        public EConnectionType connectionType;

        public SNodeConnection(AiMovementNode first, AiMovementNode second, EConnectionType connectionType)
        {
            this.first = first;
            this.second = second;
            this.connectionType = connectionType;
        }

        public bool Contains(AiMovementNode node)
        {
            return node == first || node == second;
        }

        public Vector3 GetLeftmost()
        {
            Vector3 a = first.transform.position;
            Vector3 b = second.transform.position;
            Vector3 ret;
            if (a.x < b.x)
            {
                ret = a;
            }
            else
            {
                ret = b;
            }
            return ret;
        }

        public Vector3 GetRightmost()
        {
            Vector3 a = first.transform.position;
            Vector3 b = second.transform.position;
            Vector3 ret;
            if (a.x >= b.x)
            {
                ret = a;
            }
            else
            {
                ret = b;
            }
            return ret;
        }

        public float SquareDistanceFrom(Vector3 position)
        {
            float ret = 0.0f;
            Vector3 pointOnLine = ProjectPointOnLineSegment(first.transform.position, second.transform.position, position);
            ret = Vector3.SqrMagnitude(position - pointOnLine);
            return ret;
        }

        public Vector3 GetClosestPointOnLine(Vector3 position, bool clampEnds)
        {
            Vector3 ret = Vector3.zero;
            if (clampEnds)
            {
                ret = ProjectPointOnLineSegment(first.transform.position, second.transform.position, position);
            }
            else
            {
                ret = Vector3.Project(position - second.transform.position, first.transform.position - second.transform.position) + second.transform.position;
            }
            return ret;
        }
    }
    private List<SNodeConnection> allConnections = new List<SNodeConnection>();

    #region Singleton

    // This could be static, but making it a component should make it easier to get scene events and clean it up when loading a new level.
    private static AiMovementNodeManager singleton = null;
    public static bool TryGetInstance(out AiMovementNodeManager manager)
    {
        if (singleton == null)
        {
            GameObject singletonGameObject = new GameObject();
            singleton = singletonGameObject.AddComponent<AiMovementNodeManager>();
            singletonGameObject.name = "AiMovementNodeManager";
        }
        manager = singleton;
        return true;
    }

    #endregion

    public void AddConnection(AiMovementNode first, AiMovementNode second, EConnectionType connectionType)
    {
        allConnections.Add(new SNodeConnection(first, second, connectionType));
    }

    public SNodeConnection GetNearestConnection(Vector3 position)
    {
        SNodeConnection ret = null;
        float bestDistance = float.PositiveInfinity;
        foreach (SNodeConnection connectionToCheck in allConnections)
        {
            float squareDistance = connectionToCheck.SquareDistanceFrom(position);
            if (squareDistance < bestDistance)
            {
                ret = connectionToCheck;
                bestDistance = squareDistance;
            }
        }
        return ret;
    }

    #region Math Functions

    public static Vector3 ProjectPointOnLineSegment(Vector3 linePoint1, Vector3 linePoint2, Vector3 point)
    {
        Vector3 vector = linePoint2 - linePoint1;

        Vector3 projectedPoint = ProjectPointOnLine(linePoint1, vector.normalized, point);

        int side = PointOnWhichSideOfLineSegment(linePoint1, linePoint2, projectedPoint);

        //The projected point is on the line segment
        if (side == 0)
        {

            return projectedPoint;
        }

        if (side == 1)
        {

            return linePoint1;
        }

        if (side == 2)
        {

            return linePoint2;
        }

        //output is invalid
        return Vector3.zero;
    }

    public static Vector3 ProjectPointOnLine(Vector3 linePoint, Vector3 lineVec, Vector3 point)
    {

        //get vector from point on line to point in space
        Vector3 linePointToPoint = point - linePoint;

        float t = Vector3.Dot(linePointToPoint, lineVec);

        return linePoint + lineVec * t;
    }

    public static int PointOnWhichSideOfLineSegment(Vector3 linePoint1, Vector3 linePoint2, Vector3 point)
    {

        Vector3 lineVec = linePoint2 - linePoint1;
        Vector3 pointVec = point - linePoint1;

        float dot = Vector3.Dot(pointVec, lineVec);

        //point is on side of linePoint2, compared to linePoint1
        if (dot > 0)
        {

            //point is on the line segment
            if (pointVec.magnitude <= lineVec.magnitude)
            {

                return 0;
            }

            //point is not on the line segment and it is on the side of linePoint2
            else
            {

                return 2;
            }
        }

        //Point is not on side of linePoint2, compared to linePoint1.
        //Point is not on the line segment and it is on the side of linePoint1.
        else
        {

            return 1;
        }
    }
    #endregion

}
