using System;
using System.Collections.Generic;
using System.Numerics;

namespace AltV.Net.EntitySync.SpatialPartitions
{
    public class Grid3 : SpatialPartition
    {
        private static readonly float Tolerance = 0.013F; //0.01318359375F;

        // x-index, y-index, col shapes
        protected readonly List<IEntity>[][] entityAreas;

        protected readonly int maxX;

        protected readonly int maxY;

        //TODO: evaluate if float is needed because of calculations
        protected readonly int areaSize;

        protected readonly int xOffset;

        protected readonly int yOffset;

        protected readonly int maxXAreaIndex;

        protected readonly int maxYAreaIndex;
        
        private readonly IList<IEntity> entities = new List<IEntity>();

        /// <summary>
        /// The constructor of the grid spatial partition algorithm
        /// </summary>
        /// <param name="maxX">The max x value</param>
        /// <param name="maxY">The max y value</param>
        /// <param name="areaSize">The Size of a grid area</param>
        /// <param name="xOffset"></param>
        /// <param name="yOffset"></param>
        public Grid3(int maxX, int maxY, int areaSize, int xOffset, int yOffset)
        {
            this.maxX = maxX + xOffset;
            this.maxY = maxY + yOffset;
            this.areaSize = areaSize;
            this.xOffset = xOffset;
            this.yOffset = yOffset;

            maxXAreaIndex = this.maxX / areaSize;
            maxYAreaIndex = this.maxY / areaSize;

            entityAreas = new List<IEntity>[maxXAreaIndex][];

            for (var i = 0; i < maxXAreaIndex; i++)
            {
                entityAreas[i] = new List<IEntity>[maxYAreaIndex];
                for (var j = 0; j < maxYAreaIndex; j++)
                {
                    entityAreas[i][j] = new List<IEntity>();
                }
            }
        }

        public ulong GetEntityCount()
        {
            ulong count = 0;
            for (var i = 0; i < maxXAreaIndex; i++)
            {
                for (var j = 0; j < maxYAreaIndex; j++)
                {
                    count += (ulong) entityAreas[i][j].Count;
                }
            }

            return count;
        }

        //TODO: insert entities sorted by id
        public override void Add(IEntity entity)
        {
            var range = entity.Range;
            
            if (range == 0) return;

            var entityPositionX = entity.Position.X + xOffset;
            var entityPositionY = entity.Position.Y + yOffset;
            
            var squareMaxX = entityPositionX + range;
            var squareMaxY = entityPositionY + range;
            var squareMinX = entityPositionX - range;
            var squareMinY = entityPositionY - range;

            // we actually have a circle but we use this as a square for performance reasons
            // we now find all areas that are inside this square
            // We first use starting y index to start filling
            var startingYIndex =  Math.Max((int) Math.Floor(squareMinY / areaSize), 0);
            // We now define starting x index to start filling
            var startingXIndex =  Math.Max((int) Math.Floor(squareMinX / areaSize), 0);
            // Also define stopping indexes
            var stoppingYIndex =
                Math.Min((int) Math.Ceiling(squareMaxY / areaSize), maxYAreaIndex - 1);
            var stoppingXIndex =
                Math.Min((int) Math.Ceiling(squareMaxX / areaSize), maxXAreaIndex - 1);

            entity.StartingYIndex = startingYIndex;
            entity.StartingXIndex = startingXIndex;
            entity.StoppingYIndex = stoppingYIndex;
            entity.StoppingXIndex = stoppingXIndex;
            
            // Now fill all areas from min {x, y} to max {x, y}
            for (var j = startingXIndex; j <= stoppingXIndex; j++)
            {
                var xArr = entityAreas[j];
                for (var i = startingYIndex; i <= stoppingYIndex; i++)
                {
                    xArr[i].Add(entity);
                }
            }
        }

        //TODO: remove entities thar are sorted by id with binary search
        public override void Remove(IEntity entity)
        {
            var range = entity.Range;
            
            if (range == 0) return;
            
            var id = entity.Id;
            var type = entity.Type;

            // we actually have a circle but we use this as a square for performance reasons
            // we now find all areas that are inside this square
            // We first use starting y index to start filling
            var startingYIndex = entity.StartingYIndex;
            // We now define starting x index to start filling
            var startingXIndex = entity.StartingXIndex;
            // Also define stopping indexes
            var stoppingYIndex = entity.StoppingYIndex;
            var stoppingXIndex = entity.StoppingXIndex;

            // Now remove entity from all areas from min {x, y} to max {x, y}
            for (var j = startingXIndex; j <= stoppingXIndex; j++)
            {
                var xArr = entityAreas[j];
                for (var i = startingYIndex; i <= stoppingYIndex; i++)
                {
                    var arr = xArr[i];
                    var length = arr.Count;
                    int k;
                    var found = false;
                    for (k = 0; k < length; k++)
                    {
                        var currEntity = arr[k];
                        if (currEntity.Id != id || currEntity.Type != type) continue;
                        found = true;
                        break;
                    }

                    if (!found) continue;

                    //TODO: we loop the array while removing elements from it??
                    xArr[i].RemoveAt(k);
                }
            }
        }

        public override void UpdateEntityPosition(IEntity entity, in Vector3 oldPosition, in Vector3 newPosition)
        {
            var range = entity.Range;
            
            if (range == 0) return;
            
            var id = entity.Id;
            var type = entity.Type;
            
            var newEntityPositionX = newPosition.X + xOffset;
            var newEntityPositionY = newPosition.Y + yOffset;

            var newSquareMaxX = Math.Min(newEntityPositionX + range, maxX);
            var newSquareMaxY = Math.Min(newEntityPositionY + range, maxY);
            var newSquareMinX =  Math.Max(newEntityPositionX - range, 0);
            var newSquareMinY =  Math.Max(newEntityPositionY - range, 0);

            // we actually have a circle but we use this as a square for performance reasons
            // we now find all areas that are inside this square
            // We first use starting y index to start filling
            var oldStartingYIndex = entity.StartingYIndex;
            // We now define starting x index to start filling
            var oldStartingXIndex = entity.StartingXIndex;
            // Also define stopping indexes
            var oldStoppingYIndex = entity.StoppingYIndex;
            var oldStoppingXIndex = entity.StoppingXIndex;

            // we actually have a circle but we use this as a square for performance reasons
            // we now find all areas that are inside this square
            // We first use starting y index to start filling
            var newStartingYIndex = (int) Math.Floor(newSquareMinY / areaSize);
            // We now define starting x index to start filling
            var newStartingXIndex = (int) Math.Floor(newSquareMinX / areaSize);
            // Also define stopping indexes
            var newStoppingYIndex =
                (int) Math.Ceiling(newSquareMaxY / areaSize);
            var newStoppingXIndex =
                (int) Math.Ceiling(newSquareMaxX / areaSize);

            entity.StartingYIndex = newStartingYIndex;
            entity.StartingXIndex = newStartingXIndex;
            entity.StoppingYIndex = newStoppingYIndex;
            entity.StoppingXIndex = newStoppingXIndex;

            //TODO: do later checking for overlaps between the grid areas in the two dimensional array
            //  --    --    --    --   
            // |xy|  |xy|  |yy|  |  |    
            // |yx|  |yx|  |yy|  |  |
            //  --    --    --    --  
            //
            //  --    --    --    --   
            // |xy|  |xy|  |yy|  |  |    
            // |yx|  |yx|  |yy|  |  |
            //  --    --    --    --  
            //
            //  --    --    --    --   
            // |yy|  |yy|  |yy|  |  |    
            // |yy|  |yy|  |yy|  |  |
            //  --    --    --    --  
            //
            //  --    --    --    --   
            // |  |  |  |  |  |  |  |    
            // |  |  |  |  |  |  |  |
            //  --    --    --    --  
            // Now we have to check if some of the (oldStartingYIndex, oldStoppingYIndex) areas are inside (newStartingYIndex, newStoppingYIndex)
            // Now we have to check if some of the (oldStartingXIndex, oldStoppingXIndex) areas are inside (newStartingXIndex, newStoppingXIndex)


            for (var j = oldStartingXIndex; j <= oldStoppingXIndex; j++)
            {
                var xArr = entityAreas[j];
                for (var i = oldStartingYIndex; i <= oldStoppingYIndex; i++)
                {
                    //TODO: Now we check if (i,j) is inside the new position range, so we don't have to delete it
                    var arr = xArr[i];
                    var length = arr.Count;
                    int k;
                    var found = false;
                    for (k = 0; k < length; k++)
                    {
                        var currEntity = arr[k];
                        if (currEntity.Id != id || currEntity.Type != type) continue;
                        found = true;
                        break;
                    }

                    if (!found) continue;
                    xArr[i].RemoveAt(k);
                }
            }

            for (var j = newStartingXIndex; j <= newStoppingXIndex; j++)
            {
                var xArr = entityAreas[j];
                for (var i = newStartingYIndex; i <= newStoppingYIndex; i++)
                {
                    xArr[i].Add(entity);
                }
            }
        }

        public override void UpdateEntityRange(IEntity entity, uint oldRange, uint newRange)
        {
            if (newRange == 0) return;
            if (oldRange == 0) return;
            
            var id = entity.Id;
            var type = entity.Type;
            
            var entityPositionX = entity.Position.X + xOffset;
            var entityPositionY = entity.Position.Y + yOffset;

            var newSquareMaxX = Math.Min(entityPositionX + newRange, maxX);
            var newSquareMaxY = Math.Min(entityPositionY + newRange, maxY);
            var newSquareMinX =  Math.Max(entityPositionX - newRange, 0);
            var newSquareMinY =  Math.Max(entityPositionY - newRange, 0);

            // we actually have a circle but we use this as a square for performance reasons
            // we now find all areas that are inside this square
            // We first use starting y index to start filling
            var oldStartingYIndex = entity.StartingYIndex;
            // We now define starting x index to start filling
            var oldStartingXIndex = entity.StartingXIndex;
            // Also define stopping indexes
            var oldStoppingYIndex = entity.StoppingYIndex;
            var oldStoppingXIndex = entity.StoppingXIndex;

            // we actually have a circle but we use this as a square for performance reasons
            // we now find all areas that are inside this square
            // We first use starting y index to start filling
            var newStartingYIndex = (int) Math.Floor(newSquareMinY / areaSize);
            // We now define starting x index to start filling
            var newStartingXIndex = (int) Math.Floor(newSquareMinX / areaSize);
            // Also define stopping indexes
            var newStoppingYIndex =
                (int) Math.Ceiling(newSquareMaxY / areaSize);
            var newStoppingXIndex =
                (int) Math.Ceiling(newSquareMaxX / areaSize);
            
            entity.StartingYIndex = newStartingYIndex;
            entity.StartingXIndex = newStartingXIndex;
            entity.StoppingYIndex = newStoppingYIndex;
            entity.StoppingXIndex = newStoppingXIndex;

            for (var j = oldStartingXIndex; j <= oldStoppingXIndex; j++)
            {
                var xArr = entityAreas[j];
                for (var i = oldStartingYIndex; i <= oldStoppingYIndex; i++)
                {
                    //TODO: Now we check if (i,j) is inside the new position range, so we don't have to delete it
                    var arr = xArr[i];
                    var length = arr.Count;
                    int k;
                    var found = false;
                    for (k = 0; k < length; k++)
                    {
                        var currEntity = arr[k];
                        if (currEntity.Id != id || currEntity.Type != type) continue;
                        found = true;
                        break;
                    }

                    if (!found) continue;
                    xArr[i].RemoveAt(k);
                }
            }

            for (var j = newStartingXIndex; j <= newStoppingXIndex; j++)
            {
                var xArr = entityAreas[j];
                for (var i = newStartingYIndex; i <= newStoppingYIndex; i++)
                {
                    xArr[i].Add(entity);
                }
            }
        }

        public override void UpdateEntityDimension(IEntity entity, int oldDimension, int newDimension)
        {
            // This algorithm doesn't has different memory layout depending on dimension
        }

        /*
        X can see only X
        -X can see 0 and -X
        0 can't see -X and X
        */
        protected static bool CanSeeOtherDimension(int dimension, int otherDimension)
        {
            if (dimension > 0) return dimension == otherDimension || otherDimension == int.MinValue;
            if (dimension < 0) return otherDimension == 0 || dimension == otherDimension || otherDimension == int.MinValue;
            return otherDimension == 0 || otherDimension == int.MinValue;
        }

        //TODO: check if we can find a better way to pass a position and e.g. improve performance of this method by return type ect.
        public override IList<IEntity> Find(Vector3 position, int dimension)
        {
            var posX = position.X + xOffset;
            var posY = position.Y + yOffset;

            var xIndex = Math.Max(Math.Min((int) Math.Floor(posX / areaSize), maxXAreaIndex - 1), 0);

            var yIndex = Math.Max(Math.Min((int) Math.Floor(posY / areaSize), maxYAreaIndex - 1), 0);

            // x2 and y2 only required for complete exact range check

            /*var x2Index = (int) Math.Ceiling(posX / areaSize);

            var y2Index = (int) Math.Ceiling(posY / areaSize);*/

            var areaEntities = entityAreas[xIndex][yIndex];
            
            entities.Clear();

            for (int j = 0, innerLength = areaEntities.Count; j < innerLength; j++)
            {
                var entity = areaEntities[j];
                var distance = Vector3.DistanceSquared(entity.Position, position);
                if (distance > entity.RangeSquared ||
                    !CanSeeOtherDimension(dimension, entity.Dimension)) continue;
                entity.LastStreamInRange = distance;
                entities.Add(entity);
            }

            return entities;

            /*if (xIndex != x2Index && yIndex == y2Index)
            {
                var innerAreaEntities = entityAreas[x2Index][yIndex];

                for (int j = 0, innerLength = innerAreaEntities.Length; j < innerLength; j++)
                {
                    var entity = innerAreaEntities[j];
                    if (Vector3.Distance(entity.Position, position) > entity.Range) continue;
                    callback(entity);
                }
            } else if (xIndex == x2Index && yIndex != y2Index)
            {
                var innerAreaEntities = entityAreas[xIndex][y2Index];

                for (int j = 0, innerLength = innerAreaEntities.Length; j < innerLength; j++)
                {
                    var entity = innerAreaEntities[j];
                    if (Vector3.Distance(entity.Position, position) > entity.Range) continue;
                    callback(entity);
                }
            } else if (xIndex != x2Index && yIndex != y2Index)
            {
                var innerAreaEntities = entityAreas[x2Index][yIndex];

                for (int j = 0, innerLength = innerAreaEntities.Length; j < innerLength; j++)
                {
                    var entity = innerAreaEntities[j];
                    if (Vector3.Distance(entity.Position, position) > entity.Range) continue;
                    callback(entity);
                }
                
                innerAreaEntities = entityAreas[x2Index][y2Index];

                for (int j = 0, innerLength = innerAreaEntities.Length; j < innerLength; j++)
                {
                    var entity = innerAreaEntities[j];
                    if (Vector3.Distance(entity.Position, position) > entity.Range) continue;
                    callback(entity);
                }
                
                innerAreaEntities = entityAreas[xIndex][y2Index];

                for (int j = 0, innerLength = innerAreaEntities.Length; j < innerLength; j++)
                {
                    var entity = innerAreaEntities[j];
                    if (Vector3.Distance(entity.Position, position) > entity.Range) continue;
                    callback(entity);
                }
            }*/
        }
    }
}