﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEngine;

public class Utils
{
    public static DataPoint GetClosestPointOnLine(DataPoint start, DataPoint end, DataPoint bubble)
    {
        float xdif = end.X - start.X;
        float ydif = end.Y - start.Y;
        float line_len = Mathf.Sqrt(xdif * xdif + ydif * ydif);

        // Direction of line vector
        float directionX = xdif / line_len;
        float directionY = ydif / line_len;

        // get T val into line: Goes from 0 to 1.
        float t = directionX * (bubble.X - start.X) + directionY * (bubble.Y - start.Y);

        // get position of the closest point, p.
        float x = t * directionX + start.X;
        float y = t * directionY + start.Y;

        return new DataPoint(x, y);
    }

    public static bool IsInRadiusLineRange(DataPoint start, DataPoint end, DataPoint bubble, float triggerRadius)
    {
        return IsLineTouchingCircle(start, end, bubble, triggerRadius, 0, false);
    }

    public static bool IsLineTouchingCircle(DataPoint start, DataPoint end, DataPoint bubble, float triggerRadius, float bubbleRadius, bool cullOutsideT = true)
    {
        // Get length of line
        float xdif = end.X - start.X;
        float ydif = end.Y - start.Y;
        float line_len = Mathf.Sqrt(xdif * xdif + ydif * ydif);

        // Get directon vector components
        float directionX = xdif / line_len;
        float directionY = ydif / line_len;

        // get T val into line: Goes from 0 to 1.
        float t = directionX * (bubble.X - start.X) + directionY * (bubble.Y - start.Y);

        // if t > 1 or < 0, we're not on the line.
        // At this point, we would be either behind or in front of it.
        if (cullOutsideT)
        {
            if (t > line_len + bubbleRadius || t < 0 - bubbleRadius)
                return false;
        }

        // get position of the closest point, p.
        float x = t * directionX + start.X;
        float y = t * directionY + start.Y;

        // Check if less than radius. Start by finding distance to line.
        xdif = x - bubble.X;
        ydif = y - bubble.Y;
        float circleDistance = Mathf.Sqrt(xdif * xdif + ydif * ydif);

        // Return if we hit/pass through the circle or not.
        if (circleDistance <= triggerRadius)
            return true;

        return false;
    }

    public static void SetMaterialToColorWithAlpha(GameObject obj, Color color)
    {
        Material surfMaterial = obj.GetComponent<Renderer>().material;
        surfMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        surfMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        surfMaterial.SetInt("_ZWrite", 0);
        surfMaterial.DisableKeyword("_ALPHATEST_ON");
        surfMaterial.EnableKeyword("_ALPHABLEND_ON");
        surfMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        surfMaterial.renderQueue = 3000;
        surfMaterial.SetColor("_Color", color);
    }

    // Lightly modified.
    public class WichmannRng
    {
        int s1;
        int s2;
        int s3;

        public WichmannRng(int seed)
        {
            seed += 10;
            seed = seed % 30000;

            if (seed <= 0 || seed > 30000)
                throw new System.Exception("Bad seed");

            s1 = seed;
            s2 = seed + 1;
            s3 = seed + 2;
        }

        public double Next()
        {
            s1 = 171 * (s1 % 177) - 2 * (s1 / 177);
            if (s1 < 0) { s1 += 30269; }
            s2 = 172 * (s2 % 176) - 35 * (s2 / 176);
            if (s2 < 0) { s2 += 30307; }
            s3 = 170 * (s3 % 178) - 63 * (s3 / 178);
            if (s3 < 0) { s3 += 30323; }
            double r = (s1 * 1.0) / 30269 + (s2 * 1.0) / 30307 + (s3 * 1.0) / 30323;
            return r - System.Math.Truncate(r);
        }
    }

    /// <summary>
    /// Compresses the string.
    /// Adapted from: https://stackoverflow.com/a/17993002/5383198
    /// </summary>
    /// <param name="text">The text.</param>
    /// <returns></returns>
    public static string CompressString(string text)
    {
        byte[] buffer = Encoding.UTF8.GetBytes(text);
        var memoryStream = new MemoryStream();
        using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
        {
            gZipStream.Write(buffer, 0, buffer.Length);
        }

        memoryStream.Position = 0;

        var compressedData = new byte[memoryStream.Length];
        memoryStream.Read(compressedData, 0, compressedData.Length);

        var gZipBuffer = new byte[compressedData.Length + 4];
        Buffer.BlockCopy(compressedData, 0, gZipBuffer, 4, compressedData.Length);
        Buffer.BlockCopy(BitConverter.GetBytes(buffer.Length), 0, gZipBuffer, 0, 4);
        return Convert.ToBase64String(gZipBuffer);
    }

    /// <summary>
    /// Decompresses the string.
    /// Adapted from: https://stackoverflow.com/a/17993002/5383198
    /// </summary>
    /// <param name="compressedText">The compressed text.</param>
    /// <returns></returns>
    public static string DecompressString(string compressedText)
    {
        byte[] gZipBuffer = Convert.FromBase64String(compressedText);
        using (var memoryStream = new MemoryStream())
        {
            int dataLength = BitConverter.ToInt32(gZipBuffer, 0);
            memoryStream.Write(gZipBuffer, 4, gZipBuffer.Length - 4);

            var buffer = new byte[dataLength];

            memoryStream.Position = 0;
            using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
            {
                gZipStream.Read(buffer, 0, buffer.Length);
            }

            return Encoding.UTF8.GetString(buffer);
        }
    }
}
