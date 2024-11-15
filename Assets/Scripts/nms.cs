using System;
using System.Collections.Generic;
using System.Linq;

namespace radar.utils
{
    public class BoundingBox
    {
        public float XMin { get; set; }
        public float YMin { get; set; }
        public float XMax { get; set; }
        public float YMax { get; set; }
        public float Confidence { get; set; }
    }

    class NMS
    {
        public static float IntersectionOverUnion(BoundingBox box1, BoundingBox box2)
        {
            float x1 = Math.Max(box1.XMin, box2.XMin);
            float y1 = Math.Max(box1.YMin, box2.YMin);
            float x2 = Math.Min(box1.XMax, box2.XMax);
            float y2 = Math.Min(box1.YMax, box2.YMax);

            float intersectionArea = Math.Max(0, x2 - x1 + 1) * Math.Max(0, y2 - y1 + 1);

            float box1Area = (box1.XMax - box1.XMin + 1) * (box1.YMax - box1.YMin + 1);
            float box2Area = (box2.XMax - box2.XMin + 1) * (box2.YMax - box2.YMin + 1);

            float iou = intersectionArea / (box1Area + box2Area - intersectionArea);
            return iou;
        }

        public static List<BoundingBox> NonMaxSuppression(List<BoundingBox> boxes, float threshold)
        {
            List<BoundingBox> pickedBoxes = new List<BoundingBox>();

            // 根据置信度对边界框进行排序
            boxes = boxes.OrderByDescending(box => box.Confidence).ToList();

            while (boxes.Count > 0)
            {
                // 选择具有最高置信度的边界框
                BoundingBox topBox = boxes[0];
                pickedBoxes.Add(topBox);
                boxes.RemoveAt(0);

                // 删除与所选框重叠面积大于阈值的其他框
                List<BoundingBox> overlappingBoxes = new List<BoundingBox>();
                foreach (BoundingBox box in boxes)
                {
                    if (IntersectionOverUnion(topBox, box) > threshold)
                    {
                        overlappingBoxes.Add(box);
                    }
                }
                foreach (BoundingBox box in overlappingBoxes)
                {
                    boxes.Remove(box);
                }
            }

            return pickedBoxes;
        }
    }
}