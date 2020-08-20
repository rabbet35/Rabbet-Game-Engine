﻿using FredrickTechDemo.FredsMath;

namespace FredrickTechDemo
{
    public struct PointCollider : ICollider
    {
        public Vector3D pos;
        public Entity parent;

        public PointCollider(Vector3D pos, Entity parent = null)
        {
            this.pos = pos;
            this.parent = parent;
        }
     
        public bool getHasParent()
        {
            return parent != null;
        }

        public ICollider getNextTickPredictedHitbox()
        {
            if (parent != null)
            {
                PointCollider result = new PointCollider(pos);
                result.pos += parent.getPredictedNextTickPos();
                return result;
            }
            return this;
        }

        public Entity getParent()
        {
            return parent;
        }

        public void onTick()
        {
            pos = parent.getPosition();
        }

        public ColliderType getType()
        {
            return ColliderType.point;
        }
    }
}
