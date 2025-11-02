// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Jonas00000

using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Base.Customs;

namespace AutoLighterV2
{
    internal static class EventGenerator
    {
        private static float Clamp(float v, float min, float max) => v < min ? min : (v > max ? max : v);

        private sealed class QBBrightness
        {
            public int StartQB { get; }
            public List<float> Values { get; }

            public QBBrightness(int startQB, List<float> values)
            {
                StartQB = startQB;
                Values = values;
            }
        }

        private static QBBrightness BuildQuarterBeatBrightness(MapEditorState state, AutoLightV2Config cfg)
        {
            var noteBeats = state.Notes.Where(n => n.Type != 3).OrderBy(n => n.JsonTime).Select(n => n.JsonTime).ToList();
            if (noteBeats.Count == 0) return null;
            float first = noteBeats.First();
            float last = noteBeats.Last();
            int qbStart = (int)Math.Floor(first * 4);
            int qbEnd = (int)Math.Ceiling(last * 4);

            var counts = new List<int>(qbEnd - qbStart + 1);
            int leftBeatIdx = 0, rightBeatIdx = 0;
            int windowSize = 4;

            for (int curQuarterBeat = 0; curQuarterBeat <= qbEnd; curQuarterBeat++)
            {
                float center = curQuarterBeat / 4f;
                float left = center - windowSize / 2f;
                float right = center + windowSize / 2f;

                while (rightBeatIdx < noteBeats.Count && noteBeats[rightBeatIdx] <= right)
                {
                    rightBeatIdx++;
                }
                while (leftBeatIdx < noteBeats.Count && noteBeats[leftBeatIdx] < left)
                {
                    leftBeatIdx++;
                }
                counts.Add(rightBeatIdx - leftBeatIdx);
            }

            if (counts.Count == 0) return null;
            float minVal = float.MaxValue, maxVal = float.MinValue;
            for (int i = 0; i < counts.Count; i++)
            {
                if (counts[i] < minVal) minVal = counts[i];
                if (counts[i] > maxVal) maxVal = counts[i];
            }

            var mapped = new List<float>(counts.Count);
            for (int i = 0; i < counts.Count; i++)
            {
                float norm = (counts[i] - minVal) / (maxVal - minVal);
                float brightness = cfg.MinBrightness + norm * (cfg.MaxBrightness - cfg.MinBrightness);
                mapped.Add(Clamp(brightness, cfg.MinBrightness, cfg.MaxBrightness));
            }

            return new QBBrightness(qbStart, mapped);
        }

        private static float AvgDistance(List<float> beats)
        {
            if (beats == null || beats.Count < 2) return 0f;
            float sum = 0f;
            int c = 0;
            for (int i = 1; i < beats.Count; i++)
            {
                sum += beats[i] - beats[i - 1];
                c++;
            }

            return c == 0 ? 0f : sum / c;
        }

        private static List<List<float>> NerfFlicker(List<List<float>> intervals, AutoLightV2Config cfg)
        {
            intervals = intervals.OrderBy(iv => iv.First()).ToList();
            int i = 0;
            while (i < intervals.Count - 1)
            {
                if (intervals[i][1] - intervals[i][0] < cfg.AntiFlickerThreshold)
                {
                    var merged = new List<float> { intervals[i][0], intervals[i + 1].Last() };
                    intervals[i] = merged;
                    intervals.RemoveAt(i + 1);
                }
                else i++;
            }

            return intervals;
        }

        private static List<List<float>> GetWallStrobes(List<BaseObstacle> walls)
        {
            var shortBeats = walls.Where(w => w.Duration < 0.25f).Select(w => w.JsonTime).Distinct().OrderBy(b => b)
                .ToList();
            var strobes = new List<List<float>>();
            float? last = null;
            var cur = new List<float>();
            foreach (var b in shortBeats)
            {
                if (last == null || b - last < 0.25f)
                {
                    cur.Add(b);
                }
                else
                {
                    if (cur.Count >= 3) strobes.Add(new List<float>(cur));
                    cur.Clear();
                    cur.Add(b);
                }

                last = b;
            }

            if (cur.Count >= 3) strobes.Add(cur);
            return strobes;
        }

        private static List<float> GetSprinkles(List<BaseObstacle> walls, AutoLightV2Config cfg)
        {
            var shortBeats = walls.Where(w => w.Duration < 1f / 3f).Select(w => w.JsonTime).Distinct().OrderBy(b => b).ToList();
            if (!cfg.WallStrobes) return shortBeats;
            {
                var res = new List<float>();
                float? last = null;
                foreach (var b in shortBeats)
                {
                    if (last == null || b - last >= 0.25f) res.Add(b);
                    last = b;
                }
                return res;
            }
        }

        private static (List<List<float>> left, List<List<float>> right) GetArcIntervals(List<BaseSlider> arcs)
        {
            var left = new List<List<float>>();
            var right = new List<List<float>>();
            foreach (var a in arcs)
            {
                float start = a.JsonTime;
                float end = (a as BaseArc)?.TailJsonTime ?? a.TailJsonTime;
                var list = a.Color == 0 ? left : right;
                list.Add(new List<float> { start, end });
            }

            left = MergeTouching(left);
            right = MergeTouching(right);
            return (left, right);
        }

        private static List<List<float>> MergeTouching(List<List<float>> iv)
        {
            iv = iv.OrderBy(x => x[0]).Select(x => new List<float>(x)).ToList();
            int i = 0;
            while (i < iv.Count - 1)
            {
                if (Math.Abs(iv[i][iv[i].Count - 1] - iv[i + 1][0]) < 1e-6)
                {
                    iv[i].Add(iv[i + 1][1]);
                    iv.RemoveAt(i + 1);
                }
                else i++;
            }

            return iv;
        }

        private static List<List<float>> GetWallIntervals(List<BaseObstacle> walls, AutoLightV2Config cfg)
        {
            var left = new List<List<float>>();
            var right = new List<List<float>>();
            foreach (var w in walls)
            {
                var list = w.PosX <= 1 ? left : right;
                if (w.Duration >= cfg.MinWallLength && (!cfg.WallStrobes || w.Duration >= 0.25f) &&
                    (!cfg.WallSprinkles || w.Duration >= 1f / 3f))
                {
                    list.Add(new List<float> { w.JsonTime, w.JsonTime + w.Duration });
                }
            }

            left = MergeOverlap(left);
            right = MergeOverlap(right);
            var merged = left.Concat(right).OrderBy(x => x[0]).ToList();
            int i = 0;
            while (i < merged.Count - 1)
            {
                if (merged[i][1] >= merged[i + 1][0])
                {
                    merged[i] = merged[i].Take(merged[i].Count - 1).Concat(merged[i + 1]).ToList();
                    merged.RemoveAt(i + 1);
                }
                else i++;
            }

            return merged;
        }

        private static List<List<float>> MergeOverlap(List<List<float>> iv)
        {
            iv = iv.OrderBy(x => x[0]).ToList();
            int i = 0;
            while (i < iv.Count - 1)
            {
                if (iv[i][1] >= iv[i + 1][0])
                {
                    iv[i][1] = Math.Max(iv[i][1], iv[i + 1][1]);
                    iv.RemoveAt(i + 1);
                }
                else i++;
            }

            return iv;
        }

        private static List<List<float>> GetBlockLaserIntervals(List<BaseNote> colorNotes)
        {
            var beats = colorNotes.Select(n => n.JsonTime).Distinct().OrderBy(b => b).ToList();
            var avg = AvgDistance(beats);
            var intervals = new List<List<float>>();
            // for (int i = 2; i < beats.Count; i++)
            // {
            //     if (beats[i - 1] - beats[i - 2] > avg)
            //     {
            //         if (beats[i] - beats[i - 1] > avg) intervals.Add(new List<float> { beats[i - 2], beats[i - 1] });
            //         else intervals.Add(new List<float> { beats[i - 2], beats[i - 1], beats[i] });
            //     }
            // }
            for (int i = 1; i < beats.Count; i++)
            {
                if (beats[i] - beats[i - 1] > avg)
                    intervals.Add(new List<float> { beats[i - 1], beats[i] });
            }

            if (beats.Count > 0)
                intervals.Add(new List<float> { beats.Last(), beats.Last() + 2 }); // potentially after song end
            return intervals;
        }

        private static List<List<float>> GetBlockShortIntervals(List<BaseNote> colorNotes)
        {
            var beats = colorNotes.Select(n => n.JsonTime).Distinct().OrderBy(b => b).ToList();
            var avg = AvgDistance(beats);
            var intervals = new List<List<float>>();
            // for (int i = 2; i < beats.Count; i++)
            // {
            //     if (beats[i - 1] - beats[i - 2] < avg)
            //     {
            //         if (beats[i] - beats[i - 1] < avg) intervals.Add(new List<float> { beats[i - 2], beats[i - 1] });
            //         else intervals.Add(new List<float> { beats[i - 2], beats[i - 1], beats[i] });
            //     }
            // }
            for (int i = 1; i < beats.Count; i++)
            {
                if (beats[i] - beats[i - 1] <= avg)
                    intervals.Add(new List<float> { beats[i - 1], beats[i] });
            }

            return intervals;
        }

        private static void RemoveEventsWhileStrobe(List<float> s, List<List<float>> lst)
        {
            int i = 0;
            while (i < lst.Count)
            {
                if (s[0] <= lst[i][0] && lst[i][0] <= s.Last() || s[0] <= lst[i].Last() && lst[i].Last() <= s.Last())
                    lst.RemoveAt(i);
                else i++;
            }
        }

        private static void RemoveEventsWhileSprinkle(float s, List<List<float>> lst)
        {
            int i = 0;
            while (i < lst.Count)
            {
                if (lst[i][0] <= s && s <= lst[i].Last()) lst.RemoveAt(i);
                else i++;
            }
        }

        private static (List<List<float>> left, List<List<float>> right) GetLasers(List<BaseSlider> arcs,
            List<BaseObstacle> walls, List<List<float>> shortLasers, List<BaseNote> colorNotes, AutoLightV2Config cfg)
        {
            var (left, right) = GetArcIntervals(arcs);
            var block = GetBlockLaserIntervals(colorNotes);
            block.AddRange(shortLasers);
            for (int i = 0; i < block.Count; i++)
            {
                if (i % 2 == 0) left.Add(block[i]);
                else right.Add(block[i]);
            }

            if (cfg.UseWalls)
            {
                var wls = GetWallIntervals(walls, cfg);
                left.AddRange(wls.Select(x => new List<float>(x)));
                right.AddRange(wls.Select(x => new List<float>(x)));
            }

            left = MergeAndDedup(left);
            right = MergeAndDedup(right);
            foreach (var side in new[] { left, right })
            {
                foreach (var iv in side)
                {
                    int i = 1;
                    while (i < iv.Count - 1)
                    {
                        if (iv[i + 1] - iv[i] < cfg.AntiFlickerThreshold)
                        {
                            float p1 = BeatPrecision(iv[i]);
                            float p2 = BeatPrecision(iv[i + 1]);
                            var pref = new[] { 0f, 0.5f, 0.333f, 0.25f, 0.125f };
                            int idx1 = 5, idx2 = 5;
                            for (int j = 0; j < pref.Length; j++)
                            {
                                if (Math.Abs(p1 - pref[j]) < 0.01f) idx1 = j;
                                if (Math.Abs(p2 - pref[j]) < 0.01f) idx2 = j;
                            }

                            if (idx1 <= idx2) iv.RemoveAt(i + 1);
                            else iv.RemoveAt(i);
                        }
                        else i++;
                    }
                }
            }

            return (left, right);
        }

        private static List<List<float>> MergeAndDedup(List<List<float>> iv)
        {
            iv = iv.OrderBy(x => x[0]).Select(x => x.Distinct().OrderBy(v => v).ToList()).ToList();
            int i = 0;
            while (i < iv.Count - 1)
            {
                if (iv[i].Last() >= iv[i + 1].First())
                {
                    iv[i] = iv[i].Concat(iv[i + 1]).Distinct().OrderBy(v => v).ToList();
                    iv.RemoveAt(i + 1);
                }
                else
                {
                    iv[i] = iv[i].Distinct().OrderBy(v => v).ToList();
                    i++;
                }
            }

            if (iv.Count > 0)
            {
                iv[iv.Count - 1] = iv[iv.Count - 1].Distinct().OrderBy(v => v).ToList();
            }

            return iv;
        }

        private static float BeatPrecision(float b)
        {
            var m = b % 1f;
            return m < 0.5f ? m : 1f - m;
        }

        private static List<List<float>> GetBottomLights(List<BaseNote> notes)
        {
            var sorted = notes.OrderBy(n => n.JsonTime).ToList();
            var beats = new List<float>();
            for (int i = 1; i < sorted.Count; i++)
            {
                if (Math.Abs(sorted[i].JsonTime - sorted[i - 1].JsonTime) < 1e-6)
                    beats.Add(sorted[i].JsonTime);
            }

            beats = beats.Distinct().OrderBy(b => b).ToList();
            var intervals = new List<List<float>>();
            if (beats.Count < 2) return intervals;
            float avg = AvgDistance(beats);
            float length = avg < 4f ? 1f : 2f;
            for (int i = 1; i < beats.Count; i++)
            {
                if (beats[i] - beats[i - 1] <= length)
                    intervals.Add(new List<float> { beats[i - 1], beats[i] });
                else
                    intervals.Add(new List<float> { beats[i - 1], beats[i - 1] + length });
            }

            return intervals;
        }

        private static int GetLaserSpeed(float beat, QBBrightness qb, AutoLightV2Config cfg)
        {
            int qbIndex = (int)Math.Round(beat * 4) - qb.StartQB;
            qbIndex = Math.Max(0, Math.Min(qb.Values.Count - 1, qbIndex));
            float intensity = (qb.Values[qbIndex] - cfg.MinBrightness) / (cfg.MaxBrightness - cfg.MinBrightness);
            return (int)Math.Max(1, Math.Round(intensity * 8 * cfg.LaserSpeedMulti));
        }

        public static List<BaseEvent> GenerateAll(MapEditorState state, AutoLightV2Config cfg)
        {
            // Gather map objects
            var notes = state.Notes.Where(n => n.Type == 0 || n.Type == 1)
                .OrderBy(n => n.JsonTime).ToList();
            var bombs = state.Notes.Where(n => n.Type == 3).ToList();
            var obstacles = state.Obstacles.OrderBy(o => o.JsonTime).ToList();
            var sliders = state.Sliders.OrderBy(s => s.JsonTime).ToList();

            // Build per-quarter-beat brightness map
            var qb = BuildQuarterBeatBrightness(state, cfg);

            // base intervals
            var bl = GetBottomLights(notes);
            var sl = GetBlockShortIntervals(notes);

            List<BaseEvent> result = new List<BaseEvent>();

            var rnd = new Random(0);

            // strobes/sprinkles from walls
            if (cfg.WallStrobes)
            {
                var strobes = GetWallStrobes(obstacles);
                foreach (var s in strobes)
                {
                    RemoveEventsWhileStrobe(s, sl);
                    if (!cfg.StrobesCenterOnly) RemoveEventsWhileStrobe(s, bl);
                }

                result.AddRange(GenerateStrobeEvents(strobes, cfg, qb, rnd));
            }

            if (cfg.WallSprinkles)
            {
                var spr = GetSprinkles(obstacles, cfg);
                if (cfg.WallStrobes)
                {
                    var strobes = GetWallStrobes(obstacles);
                    int i = 0;
                    while (i < spr.Count)
                    {
                        bool remove = false;
                        foreach (var st in strobes)
                        {
                            if (st.Contains(spr[i]))
                            {
                                remove = true;
                                break;
                            }
                        }

                        if (remove) spr.RemoveAt(i);
                        else i++;
                    }
                }

                foreach (var s in spr)
                {
                    RemoveEventsWhileSprinkle(s, sl);
                    RemoveEventsWhileSprinkle(s, bl);
                }

                result.AddRange(GenerateSprinkleEvents(spr, cfg, qb, rnd));
            }

            if (cfg.AntiFlickerThreshold > 0) sl = NerfFlicker(sl, cfg);

            var shortLasers = new List<List<float>>();
            var middleLights = new List<List<float>>();
            foreach (var iv in sl)
            {
                if (rnd.NextDouble() < 0.65) middleLights.Add(iv);
                else shortLasers.Add(iv);
            }

            var (l, r) = GetLasers(sliders, obstacles, shortLasers, notes, cfg);

            if (bombs.Count > 0)
            {
                var used = l.Concat(r).Concat(middleLights).Concat(bl).ToList();
                var unlit = bombs.Select(b => b.JsonTime).Where(b => !used.Any(iv => iv[0] <= b && b <= iv.Last())).ToList();
                result.AddRange(GenerateSprinkleEvents(unlit, cfg, qb, rnd));
            }

            result.AddRange(GenerateLaserLightEvents(l, r, cfg, qb, rnd));
            result.AddRange(GenerateShortLightEvents(middleLights, cfg, qb, rnd));
            result.AddRange(GenerateBottomLightEvents(bl, cfg, qb, rnd));

            if (cfg.ColorMode == 2) OverrideColorSwitching(result, cfg);

            result.AddRange(GenerateRotationEvents(notes, cfg, qb, state.Bookmarks));
            result.AddRange(GenerateZoomEvents(notes, cfg, qb, state.Bookmarks));

            result.AddRange(GenerateBoostEvents(state, cfg, qb));

            return result.OrderBy(e => e.JsonTime).ToList();
        }

        private static float BrightAt(float beat, QBBrightness qb, AutoLightV2Config cfg)
        {
            if (qb == null || qb.Values == null || qb.Values.Count == 0 || !cfg.UseMapIntensityForBrightness)
                return cfg.MaxBrightness;
            int qbIndex = (int)Math.Round(beat * 4) - qb.StartQB;
            qbIndex = Math.Max(0, Math.Min(qb.Values.Count - 1, qbIndex));
            return (float)Math.Round(qb.Values[qbIndex], 1);
        }

        private static List<BaseEvent> GenerateLaserLightEvents(List<List<float>> left, List<List<float>> right, AutoLightV2Config cfg, QBBrightness qb, Random rnd)
        {
            var events = new List<BaseEvent>();
            foreach (var tuple in new[] { new { et = 2, side = left }, new { et = 3, side = right } })
            {
                int color = 0;
                foreach (var iv in tuple.side)
                {
                    if (iv.Count < 2) continue;
                    if (iv.Last() - iv[iv.Count - 2] > cfg.LaserFadeOutLength)
                    {
                        iv.Insert(iv.Count - 1, iv.Last() - cfg.LaserFadeOutLength);
                    }

                    events.Add(new BaseEvent { JsonTime = iv[0], Type = tuple.et, Value = 1 + 4 * color, FloatValue = BrightAt(iv[0], qb, cfg) }); // start light
                    events.Add(new BaseEvent { JsonTime = iv[0], Type = 10 + tuple.et, Value = GetLaserSpeed(iv[0], qb, cfg), FloatValue = 1 }); // start speed

                    int flipColor = color;
                    for (int i = 1; i < iv.Count - 1; i++)
                    {
                        if (cfg.LaserColorFade && i != iv.Count - 2) flipColor = 1 - flipColor;
                        if (cfg.ResetLongLaserSpeeds && i != iv.Count - 2)
                        {
                            events.Add(new BaseEvent { JsonTime = iv[i], Type = 10 + tuple.et, Value = GetLaserSpeed(iv[i], qb, cfg), FloatValue = 1 }); // speed reset
                        }
                        events.Add(new BaseEvent { JsonTime = iv[i], Type = tuple.et, Value = 4 + 4 * flipColor, FloatValue = BrightAt(iv[i], qb, cfg) }); // mid fade
                    }

                    events.Add(new BaseEvent { JsonTime = iv.Last() - 1f / 64f, Type = tuple.et, Value = 4 + 4 * color, FloatValue = 0f }); // end fade out

                    color = cfg.ColorMode == 1 ? 1 - color : rnd.Next(0, 2);
                }
            }

            return events;
        }

        private static List<BaseEvent> GenerateBottomLightEvents(List<List<float>> intervals, AutoLightV2Config cfg, QBBrightness qb, Random rnd)
        {
            var events = new List<BaseEvent>();
            int color = 0;
            foreach (var iv in intervals)
            {
                events.Add(new BaseEvent { JsonTime = iv[0], Type = 0, Value = 1 + 4 * color, FloatValue = BrightAt(iv[0], qb, cfg) });
                events.Add(new BaseEvent { JsonTime = iv[1] - 1f / 64f, Type = 0, Value = 4 + 4 * color, FloatValue = 0f });
                color = cfg.ColorMode == 1 ? 1 - color : rnd.Next(0, 2);
            }

            return events;
        }

        private static List<BaseEvent> GenerateShortLightEvents(List<List<float>> intervals, AutoLightV2Config cfg, QBBrightness qb, Random rnd)
        {
            var events = new List<BaseEvent>();
            int[] ets = { 1, 4 };
            int[] lastColor = { 0, 0 };
            for (int idx = 0; idx < intervals.Count; idx++)
            {
                var iv = intervals[idx];
                int lane = idx % 2;
                int et = ets[lane];
                if (iv.Count == 2)
                {
                    events.Add(new BaseEvent { JsonTime = iv[0], Type = et, Value = 1 + 4 * lastColor[lane], FloatValue = BrightAt(iv[0], qb, cfg) });
                    events.Add(new BaseEvent { JsonTime = iv[1] - 1f / 64f, Type = et, Value = 4 + 4 * lastColor[lane], FloatValue = 0f });
                }
                else
                {
                    events.Add(new BaseEvent { JsonTime = iv[0], Type = et, Value = 1 + 4 * lastColor[lane], FloatValue = BrightAt(iv[0], qb, cfg) });
                    events.Add(new BaseEvent { JsonTime = iv[1], Type = et, Value = 1 + 4 * lastColor[lane], FloatValue = BrightAt(iv[1], qb, cfg) });
                    events.Add(new BaseEvent { JsonTime = iv[2] - 1f / 64f, Type = et, Value = 4 + 4 * lastColor[lane], FloatValue = 0f });
                }

                if (cfg.ColorMode == 1) lastColor[lane] = 1 - lastColor[lane];
                else
                {
                    lastColor[0] = rnd.Next(0, 2);
                    lastColor[1] = rnd.Next(0, 2);
                }
            }

            return events;
        }

        private static List<BaseEvent> GenerateStrobeEvents(List<List<float>> strobes, AutoLightV2Config cfg, QBBrightness qb, Random rnd)
        {
            var events = new List<BaseEvent>();
            int color = 0;
            var lanes = cfg.StrobesCenterOnly ? new[] { 4 } : new[] { 0, 4 };
            foreach (var iv in strobes)
            {
                float avg = 0;
                for (int i = 0; i < iv.Count - 1; i++) avg += (iv[i + 1] - iv[i]);
                avg /= Math.Max(iv.Count - 1, 1);
                foreach (var et in lanes)
                {
                    if (avg > 0.2f)
                    {
                        for (int j = 0; j < iv.Count - 1; j++)
                        {
                            events.Add(new BaseEvent { JsonTime = iv[j], Type = et, Value = 3 + 4 * color, FloatValue = BrightAt(iv[j], qb, cfg) });
                            events.Add(new BaseEvent { JsonTime = iv[j] + avg / 2f, Type = et, Value = 0, FloatValue = BrightAt(iv[j] + avg / 2f, qb, cfg) });
                        }

                        events.Add(new BaseEvent { JsonTime = iv.Last(), Type = et, Value = 3 + 4 * color, FloatValue = BrightAt(iv.Last(), qb, cfg) });
                    }
                    else
                    {
                        int tp = 1;
                        foreach (var b in iv)
                        {
                            events.Add(new BaseEvent { JsonTime = b, Type = et, Value = (tp == 1) ? 3 + 4 * color : 0, FloatValue = BrightAt(b, qb, cfg) });
                            tp = 1 - tp;
                        }
                    }
                }

                color = cfg.ColorMode == 1 ? 1 - color : rnd.Next(0, 2);
            }

            return events;
        }

        private static List<BaseEvent> GenerateSprinkleEvents(List<float> sprinkles, AutoLightV2Config cfg, QBBrightness qb, Random rnd)
        {
            var events = new List<BaseEvent>();
            int[] ets = { 0, 1, 4 };
            int lastRand = -1;
            int color = 0;
            foreach (var b in sprinkles)
            {
                int r;
                do
                {
                    r = rnd.Next(0, 3);
                } while (r == lastRand);

                lastRand = r;
                events.Add(new BaseEvent { JsonTime = b, Type = ets[r], Value = 3 + (color < 4 ? 4 : 0), FloatValue = BrightAt(b, qb, cfg) });
                color = cfg.ColorMode == 1 ? (color + 1) % 8 : rnd.Next(0, 8);
            }

            return events;
        }

        private static void OverrideColorSwitching(List<BaseEvent> lights, AutoLightV2Config cfg)
        {
            lights.Sort((a, b) => a.JsonTime.CompareTo(b.JsonTime));
            int color = 0;
            if (lights.Count == 0) return;
            float start = (float)Math.Floor(lights.First().JsonTime);
            float end = (float)Math.Floor(lights.Last().JsonTime);
            for (float beat = start + cfg.ColorSwitchBeats; beat < end; beat += cfg.ColorSwitchBeats)
            {
                foreach (var lt in lights)
                {
                    if (beat - cfg.ColorSwitchBeats <= lt.JsonTime && lt.JsonTime < beat)
                    {
                        if (lt.Type == 2 || lt.Type == 3)
                        {
                            if (lt.Value > 0 && lt.Value < 5 && color == 1) lt.Value += 4;
                            else if (lt.Value > 4 && lt.Value < 9 && color == 0) lt.Value -= 4;
                        }
                        else if (lt.Type == 0 || lt.Type == 1 || lt.Type == 4)
                        {
                            if (lt.Value > 0 && lt.Value < 5 && color == 0) lt.Value += 4;
                            else if (lt.Value > 4 && lt.Value < 9 && color == 1) lt.Value -= 4;
                        }
                    }
                }

                color = 1 - color;
            }
        }

        private static List<BaseEvent> GenerateRotationEvents(List<BaseNote> notes, AutoLightV2Config cfg, QBBrightness qb, List<BaseBookmark> bookmarks)
        {
            var events = new List<BaseEvent>();
            if (notes.Count == 0) return events;
            float first = notes[0].JsonTime;
            float last = notes.Last().JsonTime;
            float threshold = (cfg.MinBrightness + cfg.MaxBrightness) / 2f;
            for (float b = AlignToBar(first, bookmarks); b < last; b += cfg.RotationInterval)
            {
                if (qb != null && cfg.DoubleAtIntenseSections)
                {
                    int idx = (int)Math.Max(0, Math.Min(qb.Values.Count - 1, Math.Round(b * 4)));
                    if (qb.Values[idx] > threshold && b + cfg.RotationInterval / 2f < last)
                        events.Add(new BaseEvent { JsonTime = b + cfg.RotationInterval / 2f, Type = 8, Value = 7, FloatValue = 1f });
                }

                events.Add(new BaseEvent { JsonTime = b, Type = 8, Value = 7, FloatValue = 1f });
            }

            return events;
        }

        private static List<BaseEvent> GenerateZoomEvents(List<BaseNote> notes, AutoLightV2Config cfg, QBBrightness qb, List<BaseBookmark> bookmarks)
        {
            var events = new List<BaseEvent>();
            if (notes.Count == 0) return events;
            float first = notes[0].JsonTime;
            float last = notes.Last().JsonTime;
            float threshold = (cfg.MinBrightness + cfg.MaxBrightness) / 2f;
            for (float b = AlignToBar(first, bookmarks); b < last; b += cfg.ZoomInterval)
            {
                if (qb != null && cfg.DoubleAtIntenseSections)
                {
                    int idx = (int)Math.Max(0, Math.Min(qb.Values.Count - 1, Math.Round(b * 4)));
                    if (qb.Values[idx] > threshold && b + cfg.ZoomInterval / 2f < last)
                        events.Add(new BaseEvent { JsonTime = b + cfg.ZoomInterval / 2f, Type = 9, Value = 1, FloatValue = 1f });
                }

                events.Add(new BaseEvent { JsonTime = b, Type = 9, Value = 1, FloatValue = 1f });
            }

            return events;
        }

        private static float AlignToBar(float firstBeat, List<BaseBookmark> bookmarks = null)
        {
            int targetMod = (int)Math.Ceiling(firstBeat) % 4;

            if (bookmarks != null && bookmarks.Count > 0)
            {
                var fullBeatBookmarks = bookmarks.Where(b => Math.Abs(b.JsonTime - Math.Round(b.JsonTime)) < 0.1f)
                    .Select(b => (int)Math.Round(b.JsonTime)).ToList();

                if (fullBeatBookmarks.Count > 0)
                {
                    var modCounts = new Dictionary<int, int> { { 0, 0 }, { 1, 0 }, { 2, 0 }, { 3, 0 } };
                    foreach (var beat in fullBeatBookmarks)
                    {
                        int mod = beat % 4;
                        modCounts[mod]++;
                    }

                    int maxCount = modCounts.Values.Max();
                    if (maxCount > 0)
                    {
                        var mostCommonMods = modCounts.Where(kv => kv.Value == maxCount).Select(kv => kv.Key).ToList();
                        if (mostCommonMods.Count == 1)
                        {
                            targetMod = mostCommonMods[0];
                        }
                    }
                }
            }

            int offset = (targetMod - (int)Math.Ceiling(firstBeat) % 4 + 4) % 4;
            return (int)Math.Ceiling(firstBeat) + offset;
        }

        private static List<BaseEvent> GenerateBoostEvents(MapEditorState state, AutoLightV2Config cfg, QBBrightness qb)
        {
            var events = new List<BaseEvent>();
            if (cfg.BoostMode == 0) return events;
            if (cfg.BoostMode == 3) return state.ExistingBoosts.ToList();

            var notes = state.Notes.Where(n => n.Type != 3).OrderBy(n => n.JsonTime).ToList();
            if (notes.Count == 0) return events;

            List<float> boostBeats;

            if (cfg.BoostMode == 2 || qb == null)
            {
                // Mode 2: periodic boosts
                float first = notes[0].JsonTime;
                float last = notes.Last().JsonTime;
                boostBeats = new List<float>();
                for (float b = AlignToBar(first, state.Bookmarks) + cfg.MinBoostLength; b < last; b += cfg.MinBoostLength)
                    boostBeats.Add(b);
            }
            else
            {
                // Mode 1: from brightness
                boostBeats = GenerateBoostBeatsFromBrightness(qb, cfg, notes[0].JsonTime, notes.Last().JsonTime);
                if (state.Bookmarks != null && state.Bookmarks.Count > 0)
                {
                    boostBeats = SnapBoostBeatsToBookmarks(boostBeats, state.Bookmarks);
                }
            }

            boostBeats.Sort();
            bool on = false;
            foreach (var b in boostBeats)
            {
                on = !on;
                events.Add(new BaseEvent { JsonTime = b, Type = 5, Value = on ? 1 : 0 });
            }

            return events;
        }

        private static List<float> GenerateBoostBeatsFromBrightness(QBBrightness qb, AutoLightV2Config cfg, float firstBeat, float lastBeat)
        {
            var boostBeats = new List<float>();

            float threshold = cfg.MaxBrightness;
            float percentBoosted = 0f;

            while (percentBoosted < cfg.BoostPercent && threshold > cfg.MinBrightness)
            {
                boostBeats.Clear();
                bool boosting = false;
                int cntAbove = 0, cntBelow = 0;
                float totalBoostTime = 0f;

                for (int i = 0; i < qb.Values.Count; i++)
                {
                    var vol = qb.Values[i];
                    if (vol > threshold)
                    {
                        cntAbove++;
                        cntBelow = 0;
                        if (!boosting && cntAbove >= cfg.MinBoostLength * 4)
                        {
                            float beatStart = (i - cfg.MinBoostLength * 4 + qb.StartQB) / 4f;
                            boostBeats.Add(beatStart);
                            boosting = true;
                        }
                    }
                    else
                    {
                        cntBelow++;
                        cntAbove = 0;
                        if (boosting && cntBelow > cfg.MinBoostLength * 4)
                        {
                            float beatEnd = (i - cfg.MinBoostLength * 4 + qb.StartQB) / 4f;
                            totalBoostTime += (beatEnd - boostBeats.Last()) * 4;
                            boostBeats.Add(beatEnd);
                            boosting = false;
                        }
                    }
                }

                if (boosting)
                {
                    totalBoostTime += ((qb.Values.Count - 1 + qb.StartQB) / 4f - boostBeats.Last()) * 4;
                    boostBeats.Add((qb.Values.Count - 1 + qb.StartQB) / 4f);
                }
                percentBoosted = totalBoostTime / qb.Values.Count;

                threshold -= 0.01f;
            }

            return boostBeats;
        }

        private static List<float> SnapBoostBeatsToBookmarks(List<float> boostBeats, List<BaseBookmark> bookmarks)
        {
            if (bookmarks == null || bookmarks.Count == 0 || boostBeats.Count == 0)
                return boostBeats;

            var result = new List<float>();
            var usedBookmarks = new HashSet<int>();

            foreach (var beat in boostBeats)
            {
                float closestBeat = beat;
                float minDistance = float.MaxValue;
                int closestBookmarkIndex = -1;

                for (int i = 0; i < bookmarks.Count; i++)
                {
                    if (usedBookmarks.Contains(i)) continue;

                    float bookmarkBeat = bookmarks[i].JsonTime;
                    float distance = Math.Abs(bookmarkBeat - beat);

                    if (distance <= 20f && distance < minDistance)
                    {
                        minDistance = distance;
                        closestBeat = bookmarkBeat;
                        closestBookmarkIndex = i;
                    }
                }

                if (closestBookmarkIndex >= 0)
                {
                    usedBookmarks.Add(closestBookmarkIndex);
                }

                result.Add(closestBeat);
            }

            return result;
        }
    }

    internal class MapEditorState
    {
        public List<BaseNote> Notes { get; set; } = new List<BaseNote>();
        public List<BaseObstacle> Obstacles { get; set; } = new List<BaseObstacle>();
        public List<BaseSlider> Sliders { get; set; } = new List<BaseSlider>();
        public List<BaseEvent> ExistingBoosts { get; set; } = new List<BaseEvent>();
        public List<BaseBookmark> Bookmarks { get; set; } = new List<BaseBookmark>();
    }
}