﻿﻿/*
 Copyright 2015 - 2015 SPrediction
 Prediction.cs is part of SPrediction
 
 SPrediction is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.
 
 SPrediction is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.
 
 You should have received a copy of the GNU General Public License
 along with SPrediction. If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
//using EloBuddy.SDK;
using LeagueSharp.Common;
using SharpDX;

namespace SPrediction
{
    /// <summary>
    /// Spacebar Prediction class
    /// </summary>
    public static class Prediction
    {
        #region Structures for prediction inputs/results

        /// <summary>
        /// Neccesary input structure for prediction calculations
        /// </summary>
        public struct Input
        {
            #region Public Properties

            public Obj_AI_Base Target;
            public float SpellDelay;
            public float SpellMissileSpeed;
            public float SpellWidth;
            public float SpellRange;
            public bool SpellCollisionable;
            public SkillshotType SpellSkillShotType;
            public List<Vector2> Path;
            public float AvgReactionTime;
            public float LastMovChangeTime;
            public float AvgPathLenght;
            public float LastAngleDiff;
            public Vector3 From;
            public Vector3 RangeCheckFrom;

            #endregion

            #region Constructors and Destructors

            public Input(Obj_AI_Base _target, LeagueSharp.Common.Spell s)
            {
                Target = _target;
                SpellDelay = s.Delay;
                SpellMissileSpeed = s.Speed;
                SpellWidth = s.Width;
                SpellRange = s.Range;
                SpellCollisionable = s.Collision;
                SpellSkillShotType = s.Type;
                Path = Target.GetWaypoints();
                if (Target is AIHeroClient)
                {
                    AIHeroClient t = Target as AIHeroClient;
                    AvgReactionTime = t.AvgMovChangeTime();
                    LastMovChangeTime = t.LastMovChangeTime();
                    AvgPathLenght = t.AvgPathLenght();
                    LastAngleDiff = t.LastAngleDiff();
                }
                else
                {
                    AvgReactionTime = 0;
                    LastMovChangeTime = 0;
                    AvgPathLenght = 0;
                    LastAngleDiff = 0;
                }
                From = s.From;
                RangeCheckFrom = s.RangeCheckFrom;
            }

            public Input(Obj_AI_Base _target, float delay, float speed, float radius, float range, bool collision, SkillshotType type, Vector3 _from, Vector3 _rangeCheckFrom)
            {
                Target = _target;
                SpellDelay = delay;
                SpellMissileSpeed = speed;
                SpellWidth = radius;
                SpellRange = range;
                SpellCollisionable = collision;
                SpellSkillShotType = type;
                Path = Target.GetWaypoints();
                if (Target is AIHeroClient)
                {
                    AIHeroClient t = Target as AIHeroClient;
                    AvgReactionTime = t.AvgMovChangeTime();
                    LastMovChangeTime = t.LastMovChangeTime();
                    AvgPathLenght = t.AvgPathLenght();
                    LastAngleDiff = t.LastAngleDiff();
                }
                else
                {
                    AvgReactionTime = 0;
                    LastMovChangeTime = 0;
                    AvgPathLenght = 0;
                    LastAngleDiff = 0;
                }
                From = _from;
                RangeCheckFrom = _rangeCheckFrom;
            }

            #endregion
        }

        /// <summary>
        /// structure for prediction results
        /// </summary>
        public struct Result
        {
            #region Public Properties

            public Input Input;
            public Obj_AI_Base Unit;
            public Vector2 CastPosition;
            public Vector2 UnitPosition;
            public HitChance HitChance;
            public Collision.Result CollisionResult;

            #endregion

            #region Constructors and Destructors

            public Result(Input inp, Obj_AI_Base unit, Vector2 castpos, Vector2 unitpos, HitChance hc, Collision.Result col)
            {
                Input = inp;
                Unit = unit;
                CastPosition = castpos;
                UnitPosition = unitpos;
                HitChance = hc;
                CollisionResult = col;
            }

            #endregion

            #region Internal Methods

            internal void Lock(bool checkDodge = true)
            {
                this.CollisionResult = Collision.GetCollisions(this.Input.From.To2D(), this.CastPosition, this.Input.SpellRange, this.Input.SpellWidth, this.Input.SpellDelay, this.Input.SpellMissileSpeed);
                this.CheckCollisions();
                this.CheckOutofRange(checkDodge);
            }

            #endregion

            #region Private Methods

            private void CheckCollisions()
            {
                if (this.Input.SpellCollisionable && (this.CollisionResult.Objects.HasFlag(Collision.Flags.Minions) || this.CollisionResult.Objects.HasFlag(Collision.Flags.YasuoWall)))
                    this.HitChance = HitChance.Collision;
            }

            private void CheckOutofRange(bool checkDodge)
            {
                if (this.Input.RangeCheckFrom.To2D().Distance(this.CastPosition) > this.Input.SpellRange - (checkDodge ? GetArrivalTime(this.Input.From.To2D().Distance(this.CastPosition), this.Input.SpellDelay, this.Input.SpellMissileSpeed) * this.Unit.MoveSpeed * (100 - ConfigMenu.MaxRangeIgnore) / 100f : 0))
                    this.HitChance = HitChance.OutOfRange;
            }

            #endregion
        }

        /// <summary>
        /// structure for aoe prediction result
        /// </summary>
        public struct AoeResult
        {
            #region Public Properties

            public Vector2 CastPosition;
            public Collision.Result CollisionResult;
            public int HitCount;

            #endregion

            #region Constructors and Destructors

            public AoeResult(Vector2 castpos, Collision.Result col, int hc)
            {
                CastPosition = castpos;
                CollisionResult = col;
                HitCount = hc;
            }

            #endregion
        }

        #endregion

        #region Internal Properties

        internal static bool blInitialized;

        #endregion

        #region Initializer Methods

        /// <summary>
        /// Initializes Prediction Services
        /// </summary>
        public static void Initialize(Menu mainMenu, string prefMenuName = "SPRED")
        {
            if (blInitialized)
                throw new Exception("SPrediction Already Initialized");

            PathTracker.Initialize();
            Collision.Initialize();
            StasisPrediction.Initialize();
            ConfigMenu.Initialize(mainMenu, prefMenuName);
            Drawings.Initialize();
            
            blInitialized = true;
        }

        public static Menu Initialize()
        {
            try
            {
                PathTracker.Initialize();
                Collision.Initialize();
                StasisPrediction.Initialize();
                Menu spredMenu = ConfigMenu.Initialize();
                Drawings.Initialize();

                blInitialized = true;
                return spredMenu;
            }
            catch
            {
                Menu m = new Menu("SPrediction", "SPREDX");
                m.AddItem(new MenuItem("PREDICTONLIST", "Prediction Method").SetValue(new StringList(new[] { "SPrediction", "Common Prediction" }, 1)));
                return m;
            }
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Gets Prediction result
        /// </summary>
        /// <param name="input">Neccesary inputs for prediction calculations</param>
        /// <returns>Prediction result as <see cref="Prediction.Result"/></returns>
        internal static Result GetPrediction(Input input)
        {
            return GetPrediction(input.Target, input.SpellWidth, input.SpellDelay, input.SpellMissileSpeed, input.SpellRange, input.SpellCollisionable, input.SpellSkillShotType, input.Path, input.AvgReactionTime, input.LastMovChangeTime, input.AvgPathLenght, input.LastAngleDiff, input.From.To2D(), input.RangeCheckFrom.To2D());
        }

        /// <summary>
        /// Gets Prediction result
        /// </summary>
        /// <param name="target">Target for spell</param>
        /// <param name="width">Spell width</param>
        /// <param name="delay">Spell delay</param>
        /// <param name="missileSpeed">Spell missile speed</param>
        /// <param name="range">Spell range</param>
        /// <param name="collisionable">Spell collisionable</param>
        /// <param name="type">Spell skillshot type</param>
        /// <param name="from">Spell casted position</param>
        /// <returns>Prediction result as <see cref="Prediction.Result"/></returns>
        internal static Result GetPrediction(AIHeroClient target, float width, float delay, float missileSpeed, float range, bool collisionable, SkillshotType type)
        {
            return GetPrediction(target, width, delay, missileSpeed, range, collisionable, type, target.GetWaypoints(), target.AvgMovChangeTime(), target.LastMovChangeTime(), target.AvgPathLenght(), target.LastAngleDiff(), ObjectManager.Player.ServerPosition.To2D(), ObjectManager.Player.ServerPosition.To2D());
        }

        /// <summary>
        /// Gets Prediction result
        /// </summary>
        /// <param name="target">Target for spell</param>
        /// <param name="width">Spell width</param>
        /// <param name="delay">Spell delay</param>
        /// <param name="missileSpeed">Spell missile speed</param>
        /// <param name="range">Spell range</param>
        /// <param name="collisionable">Spell collisionable</param>
        /// <param name="type">Spell skillshot type</param>
        /// <param name="path">Waypoints of target</param>
        /// <param name="avgt">Average reaction time (in ms)</param>
        /// <param name="movt">Passed time from last movement change (in ms)</param>
        /// <param name="avgp">Average Path Lenght</param>
        /// <param name="from">Spell casted position</param>
        /// <param name="rangeCheckFrom"></param>
        /// <returns>Prediction result as <see cref="Prediction.Result"/></returns>
        internal static Result GetPrediction(Obj_AI_Base target, float width, float delay, float missileSpeed, float range, bool collisionable, SkillshotType type, List<Vector2> path, float avgt, float movt, float avgp, float anglediff, Vector2 from, Vector2 rangeCheckFrom)
        {
            Prediction.AssertInitializationMode();

            Result result = new Result();
            result.Input = new Input(target, delay, missileSpeed, width, range, collisionable, type, from.To3D2(), rangeCheckFrom.To3D2());
            result.Unit = target;

            try
            {
                if (type == SkillshotType.SkillshotCircle)
                    range += width;

                //to do: hook logic ? by storing average movement direction etc
                if (path.Count <= 1 && movt > 100 && (Environment.TickCount - PathTracker.EnemyInfo[target.NetworkId].LastAATick > 300 || !ConfigMenu.CheckAAWindUp)) //if target is not moving, easy to hit (and not aaing)
                {
                    result.HitChance = HitChance.VeryHigh;
                    result.CastPosition = target.ServerPosition.To2D();
                    result.UnitPosition = result.CastPosition;
                    result.Lock();

                    return result;
                }

                if (target is AIHeroClient)
                {
                    if (((AIHeroClient)target).IsChannelingImportantSpell())
                    {
                        result.HitChance = HitChance.VeryHigh;
                        result.CastPosition = target.ServerPosition.To2D();
                        result.UnitPosition = result.CastPosition;
                        result.Lock();

                        return result;
                    }

                    if (Environment.TickCount - PathTracker.EnemyInfo[target.NetworkId].LastAATick < 300 && ConfigMenu.CheckAAWindUp)
                    {
                        if (target.AttackCastDelay * 1000 + PathTracker.EnemyInfo[target.NetworkId].AvgOrbwalkTime + avgt - width / 2f / target.MoveSpeed >= GetArrivalTime(target.ServerPosition.To2D().Distance(from), delay, missileSpeed))
                        {
                            result.HitChance = HitChance.High;
                            result.CastPosition = target.ServerPosition.To2D();
                            result.UnitPosition = result.CastPosition;
                            result.Lock();

                            return result;
                        }
                    }

                    //to do: find a fuking logic
                    if (avgp < 400 && movt < 100 && path.PathLength() <= avgp)
                    {
                        result.HitChance = HitChance.High;
                        result.CastPosition = path.Last();
                        result.UnitPosition = result.CastPosition;
                        result.Lock();

                        return result;
                    }
                }

                if (target.IsDashing()) //if unit is dashing
                    return GetDashingPrediction(target, width, delay, missileSpeed, range, collisionable, type, from, rangeCheckFrom);

                if (Utility.IsImmobileTarget(target)) //if unit is immobile
                    return GetImmobilePrediction(target, width, delay, missileSpeed, range, collisionable, type, from, rangeCheckFrom);

                result = WaypointAnlysis(target, width, delay, missileSpeed, range, collisionable, type, path, avgt, movt, avgp, anglediff, from);
                
                float d = result.CastPosition.Distance(target.ServerPosition.To2D());
                if (d >= (avgt - movt) * target.MoveSpeed && d >= avgp)
                    result.HitChance = HitChance.Medium;

                result.Lock();

                return result;
            }
            finally
            {
                //check if movement changed while prediction calculations
                if (!target.GetWaypoints().SequenceEqual(path))
                    result.HitChance = HitChance.Medium;
            }
        }

        /// <summary>
        /// Gets Prediction result while unit is dashing
        /// </summary>
        /// <param name="target">Target for spell</param>
        /// <param name="width">Spell width</param>
        /// <param name="delay">Spell delay</param>
        /// <param name="missileSpeed">Spell missile speed</param>
        /// <param name="range">Spell range</param>
        /// <param name="collisionable">Spell collisionable</param>
        /// <param name="type">Spell skillshot type</param>
        /// <param name="from">Spell casted position</param>
        /// <returns></returns>
        internal static Result GetDashingPrediction(Obj_AI_Base target, float width, float delay, float missileSpeed, float range, bool collisionable, SkillshotType type, Vector2 from, Vector2 rangeCheckFrom)
        {
            Result result = new Result();
            result.Input = new Input(target, delay, missileSpeed, width, range, collisionable, type, from.To3D2(), rangeCheckFrom.To3D2());
            result.Unit = target;

            if (target.IsDashing())
            {
                var dashInfo = target.GetDashInfo();
                if (dashInfo.IsBlink)
                {
                    result.HitChance = HitChance.Impossible;
                    result.CastPosition = dashInfo.EndPos;
                    return result;
                }

                result.CastPosition = GetFastUnitPosition(target, dashInfo.Path, delay, missileSpeed, from, dashInfo.Speed);
                result.HitChance = HitChance.Dashing;

                result.Lock(false);
            }
            else
            {
                result = GetPrediction(target, width, delay, missileSpeed, range, collisionable, type, target.GetWaypoints(), 0, 0, 0, 0, from, rangeCheckFrom);
                result.Lock(false);
            }
            return result;
        }

        /// <summary>
        /// Gets Prediction result while unit is immobile
        /// </summary>
        /// <param name="target">Target for spell</param>
        /// <param name="width">Spell width</param>
        /// <param name="delay">Spell delay</param>
        /// <param name="missileSpeed">Spell missile speed</param>
        /// <param name="range">Spell range</param>
        /// <param name="collisionable">Spell collisionable</param>
        /// <param name="type">Spell skillshot type</param>
        /// <param name="from">Spell casted position</param>
        /// <returns></returns>
        internal static Result GetImmobilePrediction(Obj_AI_Base target, float width, float delay, float missileSpeed, float range, bool collisionable, SkillshotType type, Vector2 from, Vector2 rangeCheckFrom)
        {
            Result result = new Result();
            result.Input = new Input(target, delay, missileSpeed, width, range, collisionable, type, from.To3D2(), rangeCheckFrom.To3D2());
            result.Unit = target;
            result.CastPosition = target.ServerPosition.To2D();
            result.UnitPosition = result.CastPosition;

            //calculate spell arrival time
            float t = delay + Game.Ping / 2000f;
            if (missileSpeed != 0)
                t += from.Distance(target.ServerPosition) / missileSpeed;

            if (type == SkillshotType.SkillshotCircle)
                t += width / target.MoveSpeed / 2f;

            if (t >= Utility.LeftImmobileTime(target))
            {
                result.HitChance = HitChance.Immobile;
                result.Lock();

                return result;
            }

            if (target is AIHeroClient)
                result.HitChance = GetHitChance(t - Utility.LeftImmobileTime(target), ((AIHeroClient)target).AvgMovChangeTime(), 0, 0, 0);
            else
                result.HitChance = HitChance.High;

            result.Lock();

            return result;
        }

        /// <summary>
        /// Get HitChance
        /// </summary>
        /// <param name="t">Arrive time to target (in ms)</param>
        /// <param name="avgt">Average reaction time (in ms)</param>
        /// <param name="movt">Passed time from last movement change (in ms)</param>
        /// <param name="avgp">Average Path Lenght</param>
        /// <returns>HitChance</returns>
        internal static HitChance GetHitChance(float t, float avgt, float movt, float avgp, float anglediff)
        {
            if (avgp > 400)
            {
                if (movt > 50)
                {
                    if (avgt >= t + Game.Ping)
                    {
                        if (anglediff < 30)
                            return HitChance.VeryHigh;
                        else
                            return HitChance.High;
                    }
                    else if (avgt - movt >= t)
                        return HitChance.Medium;
                    else
                        return HitChance.Low;
                }
                else
                    return HitChance.VeryHigh;
            }
            else
                return HitChance.High;
        }

        /// <summary>
        /// Gets spell arrival time to cast position
        /// </summary>
        /// <param name="distance">Distance from to to</param>
        /// <param name="delay">Spell delay</param>
        /// <param name="missileSpeed">Spell missile speed</param>
        /// <returns></returns>
        internal static float GetArrivalTime(float distance, float delay, float missileSpeed = 0)
        {
            if (missileSpeed != 0)
                return distance / missileSpeed + delay;

            return delay;
        }

        /// <summary>
        /// Calculates cast position with target's path
        /// </summary>
        /// <param name="target">Target for spell</param>
        /// <param name="width">Spell width</param>
        /// <param name="delay">Spell delay</param>
        /// <param name="missileSpeed">Spell missile speed</param>
        /// <param name="range">Spell range</param>
        /// <param name="collisionable">Spell collisionable</param>
        /// <param name="type">Spell skillshot type</param>
        /// <param name="path">Waypoints of target</param>
        /// <param name="avgt">Average reaction time (in ms)</param>
        /// <param name="movt">Passed time from last movement change (in ms)</param>
        /// <param name="avgp">Average Path Lenght</param>
        /// <param name="from">Spell casted position</param>
        /// <returns></returns>
        internal static Result WaypointAnlysis(Obj_AI_Base target, float width, float delay, float missileSpeed, float range, bool collisionable, SkillshotType type, List<Vector2> path, float avgt, float movt, float avgp, float anglediff, Vector2 from, float moveSpeed = 0, bool isDash = false)
        {
            if (moveSpeed == 0)
                moveSpeed = target.MoveSpeed;

            Result result = new Result();
            result.Unit = target;

            float flyTimeMax = 0f;

            if (missileSpeed != 0) //skillshot with a missile
                flyTimeMax = range / missileSpeed;

            float tMin = delay + Game.Ping / 2000f + ConfigMenu.SpellDelay / 1000f;
            float tMax = flyTimeMax + delay + Game.Ping / 1000f + ConfigMenu.SpellDelay / 1000f;
            float pathTime = 0f;
            int[] pathBounds = new int[] { -1, -1 };

            //find bounds
            for (int i = 0; i < path.Count - 1; i++)
            {
                float t = path[i + 1].Distance(path[i]) / moveSpeed;

                if (pathTime <= tMin && pathTime + t >= tMin)
                    pathBounds[0] = i;
                if (pathTime <= tMax && pathTime + t >= tMax)
                    pathBounds[1] = i;

                if (pathBounds[0] != -1 && pathBounds[1] != -1)
                    break;

                pathTime += t;
            }

            //calculate cast & unit position
            if (pathBounds[0] != -1 && pathBounds[1] != -1)
            {
                for (int k = pathBounds[0]; k <= pathBounds[1]; k++)
                {
                    Vector2 direction = (path[k + 1] - path[k]).Normalized();
                    float distance = width;
                    float extender = target.BoundingRadius;

                    if (type == SkillshotType.SkillshotLine)
                        extender = width;

                    int steps = (int)Math.Floor(path[k].Distance(path[k + 1]) / distance);
                    //split & anlyse current path
                    for (int i = 1; i < steps - 1; i++)
                    {
                        Vector2 pCenter = path[k] + (direction * distance * i);
                        Vector2 pA = pCenter - (direction * extender);
                        Vector2 pB = pCenter + (direction * extender);
                        
                        float flytime = missileSpeed != 0 ? from.Distance(pCenter) / missileSpeed : 0f;
                        float t = flytime + delay + Game.Ping / 2000f + ConfigMenu.SpellDelay / 1000f;
                        
                        Vector2 currentPosition = target.ServerPosition.To2D();

                        float arriveTimeA = currentPosition.Distance(pA) / moveSpeed;
                        float arriveTimeB = currentPosition.Distance(pB) / moveSpeed;

                        if (Math.Min(arriveTimeA, arriveTimeB) <= t && Math.Max(arriveTimeA, arriveTimeB) >= t)
                        {
                            result.HitChance = GetHitChance(t, avgt, movt, avgp, anglediff);
                            result.CastPosition = pCenter;
                            result.UnitPosition = pCenter; //+ (direction * (t - Math.Min(arriveTimeA, arriveTimeB)) * moveSpeed);
                            return result;
                        }
                    }
                }
            }

            result.HitChance = HitChance.Impossible;
            result.CastPosition = target.ServerPosition.To2D();

            return result;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets fast-predicted unit position
        /// </summary>
        /// <param name="target">Target</param>
        /// <param name="delay">Spell delay</param>
        /// <param name="missileSpeed">Spell missile speed</param>
        /// <param name="from">Spell casted position</param>
        /// <returns></returns>
        public static Vector2 GetFastUnitPosition(Obj_AI_Base target, float delay, float missileSpeed = 0, Vector2? from = null, float distanceSet = 0)
        {
            List<Vector2> path = target.GetWaypoints();
            if (from == null)
                from = ObjectManager.Player.ServerPosition.To2D();

            if (path.Count <= 1 || (target is AIHeroClient && ((AIHeroClient)target).IsChannelingImportantSpell()) || Utility.IsImmobileTarget(target))
                return target.ServerPosition.To2D();

            if (target.IsDashing())
                return target.GetDashInfo().Path.Last();

            float distance = distanceSet;

            if (distance == 0)
            {
                float targetDistance = from.Value.Distance(target.ServerPosition);
                float flyTime = targetDistance / missileSpeed;

                if (missileSpeed != 0 && path.Count == 2)
                {
                    Vector2 Vt = (path[1] - path[0]).Normalized() * target.MoveSpeed;
                    Vector2 Vs = (target.ServerPosition.To2D() - from.Value).Normalized() * missileSpeed;
                    Vector2 Vr = Vt - Vs;

                    flyTime = targetDistance / Vr.Length();
                }

                float t = flyTime + delay + Game.Ping / 2000f;
                distance = t * target.MoveSpeed;
            }

            for (int i = 0; i < path.Count - 1; i++)
            {
                float d = path[i + 1].Distance(path[i]);
                if (distance == d)
                    return path[i + 1];
                else if (distance < d)
                    return path[i] + distance * (path[i + 1] - path[i]).Normalized();
                else distance -= d;
            }

            return path.Last();
        }

        /// <summary>
        /// Gets fast-predicted unit position
        /// </summary>
        /// <param name="target">Target</param>
        /// <param name="path">Path</param>
        /// <param name="delay">Spell delay</param>
        /// <param name="missileSpeed">Spell missile speed</param>
        /// <param name="from">Spell casted position</param>
        /// <param name="moveSpeed">Move speed</param>
        /// <param name="distanceSet"></param>
        /// <returns></returns>
        public static Vector2 GetFastUnitPosition(Obj_AI_Base target, List<Vector2> path, float delay, float missileSpeed = 0, Vector2? from = null, float moveSpeed = 0, float distanceSet = 0)
        {
            if (from == null)
                from = target.ServerPosition.To2D();

            if (moveSpeed == 0)
                moveSpeed = target.MoveSpeed;

            if (path.Count <= 1 || (target is AIHeroClient && ((AIHeroClient)target).IsChannelingImportantSpell()) || Utility.IsImmobileTarget(target))
                return target.ServerPosition.To2D();

            if (target.IsDashing())
                return target.GetDashInfo().Path.Last();

            float distance = distanceSet;

            if (distance == 0)
            {
                float targetDistance = from.Value.Distance(target.ServerPosition);
                float flyTime = 0f;

                if (missileSpeed != 0) //skillshot with a missile
                {
                    Vector2 Vt = (path[path.Count - 1] - path[0]).Normalized() * moveSpeed;
                    Vector2 Vs = (target.ServerPosition.To2D() - from.Value).Normalized() * missileSpeed;
                    Vector2 Vr = Vs - Vt;

                    flyTime = targetDistance / Vr.Length();

                    if (path.Count > 5) //complicated movement
                        flyTime = targetDistance / missileSpeed;
                }

                float t = flyTime + delay + Game.Ping / 2000f + ConfigMenu.SpellDelay / 1000f;
                distance = t * moveSpeed;
            }

            for (int i = 0; i < path.Count - 1; i++)
            {
                float d = path[i + 1].Distance(path[i]);
                if (distance == d)
                    return path[i + 1];
                else if (distance < d)
                    return path[i] + distance * (path[i + 1] - path[i]).Normalized();
                else distance -= d;
            }

            return path.Last();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Initialization assert
        /// </summary>
        internal static void AssertInitializationMode()
        {
            if (!blInitialized)
                throw new InvalidOperationException("Prediction is not initalized");
        }
        
        #endregion
    }
}
