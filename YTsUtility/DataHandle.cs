﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using static System.Math;
using System.Text.RegularExpressions;
using OData = YTsUtility.PhotoGramMetry.ObjectData;
using IData = YTsUtility.PhotoGramMetry.ImageData;
using InElement = YTsUtility.PhotoGramMetry.InsideElement;
using DParams = YTsUtility.PhotoGramMetry.DistortionParams;
using OutElement=YTsUtility.PhotoGramMetry.OutsideElement;

namespace YTsUtility
{
    namespace PhotoGramMetry
    {
        /// <summary>
        /// 基于欧拉角摄影测量数据处理
        /// </summary>
        public class DataHandle
        {
            /// <summary>
            /// 控制点物方坐标
            /// </summary>
            public List<OData> ControlOList;
            /// <summary>
            /// 控制点像方坐标
            /// </summary>
            public List<List<IData>> ControlIList;
            /// <summary>
            /// 已匹配的控制点物方坐标
            /// </summary>
            public List<OData> MCOList;
            /// <summary>
            /// 已匹配的控制点像方坐标
            /// </summary>
            public List<List<IData>> MCIList;
            /// <summary>
            /// 求解出的加密点物方坐标
            /// </summary>
            public List<OData> PassOList = new List<OData>();
            /// <summary>
            /// 加密点像方坐标
            /// </summary>
            public List<List<IData>> PassIList;
            /// <summary>
            /// 匹配到的控制点数
            /// </summary>
            public int cpointsCount;
            /// <summary>
            /// 加密点物方坐标
            /// </summary>
            List<OData> KnownOList;
            /// <summary>
            /// 加密点物方坐标是否已知
            /// </summary>
            bool hasKnown;

            List<OData> AllOList;
            List<List<IData>> AllIList;

            List<OutElement> outE = new List<OutElement>();
            InElement inE;
            DParams dParams;

            /// <summary>
            /// 构造函数
            /// </summary>
            /// <param name="coList">控制点物方坐标</param>
            /// <param name="ciList">控制点物方坐标</param>
            /// <param name="piList">加密点像方坐标</param>
            public DataHandle(List<OData> coList, List<List<IData>> ciList, List<List<IData>> piList)
            {
                ControlOList = coList;
                ControlIList = ciList;
                PassIList = piList;
                hasKnown = false;
                cpointsCount = MatchCPoints();
            }

            /// <summary>
            /// 构造函数
            /// </summary>
            /// <param name="coList">控制点物方坐标</param>
            /// <param name="ciList">控制点物方坐标</param>
            /// <param name="piList">加密点像方坐标</param>
            /// <param name="koList">加密点物方坐标</param>
            public DataHandle(List<OData> coList, List<List<IData>> ciList, List<List<IData>> piList, List<OData> koList)
                : this(coList, ciList, piList)
            {
                KnownOList = koList;
                hasKnown = true;
                cpointsCount = MatchCPoints();
            }

            /// <summary>
            /// 构造函数
            /// </summary>
            /// <param name="konwnop">是否已知加密点物方坐标(用于评定精度)</param>
            /// <param name="copath">控制点物方坐标文件</param>
            /// <param name="cipath">控制点像方坐标文件</param>
            /// <param name="pipath">加密点像方坐标文件</param>
            /// <param name="overlap">像片重叠度</param>
            /// <param name="popath">加密点物方坐标文件(默认为空)</param>
            public DataHandle(bool konwnop, int overlap, string copath, string cipath, string pipath, string popath = null)
            {
                hasKnown = konwnop;
                ControlOList = new List<OData>();
                ControlIList = new List<List<IData>>();
                PassIList = new List<List<IData>>();
                PassIList = new List<List<IData>>();
                #region 读取控制点物方坐标
                StreamReader sr = new StreamReader(copath, Encoding.Default);
                while (sr.Peek() != -1)
                {
                    string s = sr.ReadLine();
                    var ss = Regex.Split(s, " +|,");
                    OData od = new OData();
                    od.Name = ss[0];
                    od.pos.X = Convert.ToDouble(ss[1]);
                    od.pos.Y = Convert.ToDouble(ss[2]);
                    od.pos.Z = Convert.ToDouble(ss[3]);
                    ControlOList.Add(od);
                }
                sr.Close();
                #endregion
                #region 读取控制点像方坐标
                for (int i = 0; i < overlap; i++)
                {
                    List<IData> id = new List<IData>();
                    ControlIList.Add(id);
                }
                sr = new StreamReader(cipath);
                while (sr.Peek() != -1)
                {
                    string s = sr.ReadLine();
                    var ss = Regex.Split(s, " +|,");
                    for (int i = 0, j = 1; i < overlap; j = j + 2, i++)
                    {
                        IData id = new IData();
                        id.Name = ss[0];
                        id.pos.X = Convert.ToDouble(ss[j]);
                        id.pos.Y = Convert.ToDouble(ss[j + 1]);
                        ControlIList[i].Add(id);
                    }
                }
                sr.Close();
                #endregion
                #region 读取加密点像方坐标
                for (int i = 0; i < overlap; i++)
                {
                    List<IData> id = new List<IData>();
                    PassIList.Add(id);
                }
                sr = new StreamReader(pipath);
                while (sr.Peek() != -1)
                {
                    string s = sr.ReadLine();
                    var ss = Regex.Split(s, " +|,");
                    for (int i = 0, j = 1; i < overlap; j = j + 2, i++)
                    {
                        IData id = new IData();
                        id.Name = ss[0];
                        id.pos.X = Convert.ToDouble(ss[j]);
                        id.pos.Y = Convert.ToDouble(ss[j + 1]);
                        PassIList[i].Add(id);
                    }
                }
                sr.Close();
                #endregion
                if (konwnop)
                {
                    KnownOList = new List<OData>();
                    #region 读取加密点物方坐标
                    sr = new StreamReader(popath, Encoding.Default);
                    while (sr.Peek() != -1)
                    {
                        string s = sr.ReadLine();
                        var ss = Regex.Split(s, " +|,");
                        OData od = new OData();
                        od.Name = ss[0];
                        od.pos.X = Convert.ToDouble(ss[1]);
                        od.pos.Y = Convert.ToDouble(ss[2]);
                        od.pos.Z = Convert.ToDouble(ss[3]);
                        KnownOList.Add(od);
                    }
                    sr.Close();
                    #endregion
                }
                cpointsCount = MatchCPoints();
            }

            /// <summary>
            /// 匹配控制点(按点名)
            /// </summary>
            /// <returns></returns>
            private int MatchCPoints()
            {
                MCIList = new List<List<IData>>();
                MCOList = new List<OData>();
                for (int i = 0; i < ControlIList.Count; ++i)
                {
                    MCIList.Add(new List<IData>());
                }//初始化
                #region 匹配控制点
                foreach (var od in ControlOList)
                {
                    var rel = from id in ControlIList[0]
                              where id.Name == od.Name
                              select id;
                    if (rel.Count() > 0)
                    {
                        MCOList.Add(od);
                        int index = ControlIList[0].IndexOf(rel.First());
                        for (int i = 0; i < ControlIList.Count; ++i)
                        {
                            MCIList[i].Add(ControlIList[i][index]);
                        }
                    }
                }
                #endregion
                return MCOList.Count;

            }

            #region 后方交会
            /// <summary>
            /// 设置初值
            /// </summary>
            /// <param name="ie">内方位元素初值</param>
            /// <param name="oeList">外方元素初值</param>
            /// <param name="dp">畸变参数</param>
            public void SetOriginValue(InElement ie, List<OutElement> oeList, DParams dp)
            {
                inE = ie;
                dParams = dp;
                outE = oeList;
            }

            /// <summary>
            /// 已知内方位元素、外方位元素、畸变参数初值的后方交会,应先调用SetOriginValue
            /// </summary>
            public void BackForcus()
            {
                Matrix B = new Matrix(MCOList.Count * 2 * MCIList.Count, 6 * MCIList.Count+9),
                       l = new Matrix(B.Row, 1);
                double[] X = new double[6 * outE.Count+9];
                for (int i = 0, j = 0; i < outE.Count; i++, j = j + 6)
                {
                    X[j] = outE[i].Spos.X;
                    X[j + 1] = outE[i].Spos.Y;
                    X[j + 2] = outE[i].Spos.Z;
                    X[j + 3] = outE[i].phi;
                    X[j + 4] = outE[i].omega;
                    X[j + 5] = outE[i].kappa;
                }
                int start = outE.Count * 6;
                X[start] = inE.p0.X;
                X[start + 1] = inE.p0.Y;
                X[start + 2] = inE.f;
                X[start + 3] = dParams.k1;
                X[start + 4] = dParams.k2;
                X[start + 5] = dParams.p1;
                X[start + 6] = dParams.p2;
                X[start + 7] = dParams.alph;
                X[start + 8] = dParams.beta;

                Matrix X0 = new Matrix(X.GetLength(0), 1, X);
                Matrix x;
                do
                {
                    SetBl(B, l);
                    x = (B.T() * B).Inverse() * B.T() * l;
                    X0 = X0 + x;
                    UpdateData(X0);
                } while (!IsTerminating(0.000001, x));
                UpdateData(X0);
            }

            private void SetBl(Matrix B, Matrix l)
            {
                DMath sin = Math.Sin, cos = Math.Cos, tan = Math.Tan;
                int count = MCOList.Count ,start=outE.Count*6;
                double x0 = inE.p0.X,
                       y0 = inE.p0.Y,
                       f = inE.f,
                       k1 = dParams.k1,
                       k2 = dParams.k2,
                       p1 = dParams.p1,
                       p2 = dParams.p2,
                       alph = dParams.alph,
                       beta = dParams.beta;
                #region 设置B,l的值
                for (int i = 0; i < outE.Count; ++i)
                {
                    var R = outE[i].GetR();
                    double phi=outE[i].phi,
                           omega=outE[i].omega,
                           kappa=outE[i].kappa,
                           Xs = outE[i].Spos.X,
                           Ys = outE[i].Spos.Y,
                           Zs = outE[i].Spos.Z,
                           a1 = R[0,0],
                           a2 = R[0, 1],
                           a3 = R[0, 2],
                           b1 = R[1, 0],
                           b2 = R[1, 1],
                           b3 = R[1, 2],
                           c1 = R[2, 0],
                           c2 = R[2, 1],
                           c3 = R[2, 2];
                    int indexj = i * 6;
                    for (int j = 0; j < count; ++j)
                    {
                        int index = i * count * 2 + 2 * j;


                        double X = MCOList[j].pos.X,
                               Y = MCOList[j].pos.Y,
                               Z = MCOList[j].pos.Z,
                               X_Xs = X - Xs,
                               Y_Ys = Y - Ys,
                               Z_Zs = Z - Zs,
                               Xr = a1 * X_Xs + b1 * Y_Ys + c1 * Z_Zs,
                               Yr = a2 * X_Xs + b2 * Y_Ys + c2 * Z_Zs,
                               Zr = a3 * X_Xs + b3 * Y_Ys + c3 * Z_Zs,
                               x = MCIList[i][j].pos.X,
                               y = MCIList[i][j].pos.Y,
                               x_x0 = x - x0,
                               y_y0 = y - y0,
                               r = _2D_Point.Get_Norm(MCIList[i][j].pos, inE.p0),
                               r_2 = r * r,
                               r_4 = r_2 * r_2,
                               dx = x_x0 * (k1 * r_2 + k2 * r_4) + p1 * (r_2 + 2 * x_x0 * x_x0) + 2 * p2 * x_x0 * y_y0 + alph * x_x0 + beta * y_y0,
                               dy = y_y0 * (k1 * r_2 + k2 * r_4) + p2 * (r_2 + 2 * y_y0 * y_y0) + 2 * p1 * x_x0 * y_y0,
                               xr = x_x0 - dx,
                               yr = y_y0 - dy;
                        B[index, indexj + 0] = (a1 * f + a3 * xr) / Zr;
                        B[index, indexj + 1] = (b1 * f + b3 * xr) / Zr;
                        B[index, indexj + 2] = (c1 * f + c3 * xr) / Zr;
                        B[index, indexj + 3] = y * sin(omega)-cos(omega) *(x * (x * cos(kappa) -y * sin(kappa)) / f +f * cos(kappa));
                        B[index, indexj + 4] = -f * sin(kappa) -x * (x * sin(kappa) + y * cos(kappa)) / f;
                        B[index, indexj + 5] = y;
                        B[index, start] = 1 - (k1 * r_2 + k2 * r_4 + 2 * k1 * x_x0 * x_x0 + 4 * k2 * r_2 * x_x0 * x_x0 + 6 * p1 * x_x0 + 2 * p2 * y_y0 + alph);
                        B[index, start + 1] = -(2 * k1 * x_x0 * y_y0 + 4 * k2 * r_2 * x_x0 * y_y0 + 2 * p1 * y_y0 + 2 * p2 * x_x0 + beta);
                        B[index, start + 2] = xr / f;
                        B[index, start + 3] = x_x0 * r_2;
                        B[index, start + 4] = r_4 * x_x0;
                        B[index, start + 5] = r_2 + 2 * x_x0 * x_x0;
                        B[index, start + 6] = 2 * x_x0 * y_y0;
                        B[index, start + 7] = x_x0;
                        B[index, start + 8] = y_y0;

                        B[index + 1, indexj + 0] = (a2 * f + a3 * yr) / Zr;
                        B[index + 1, indexj + 1] = (b2 * f + b3 * yr) / Zr;
                        B[index + 1, indexj + 2] = (c2 * f + c3 * yr) / Zr;
                        B[index + 1, indexj + 3] = -x * sin(omega) - cos(omega) *(y * (x * cos(kappa) - y * sin(kappa)) / f - f * sin(kappa));
                        B[index + 1, indexj + 4] = -f * cos(kappa) - y * (x * sin(kappa) + y * cos(kappa)) / f;
                        B[index + 1, indexj + 5] = -x;
                        B[index + 1, start] = -(2 * k1 * x_x0 * y_y0 + 4 * k2 * r_2 * x_x0 * y_y0 + 2 * p2 * x_x0 + 2 * p1 * y_y0);
                        B[index + 1, start + 1] = 1 - (k1 * r_2 + k2 * r_4 + 2 * k1 * y_y0 * y_y0 + 4 * k2 * r_2 * y_y0 * y_y0 + 6 * p2 * y_y0 + 2 * p1 * x_x0);
                        B[index + 1, start + 2] = yr / f;
                        B[index + 1, start + 3] = y_y0 * r_2;
                        B[index + 1, start + 4] = y_y0 * r_4;
                        B[index + 1, start + 5] = 2 * x_x0 * y_y0;
                        B[index + 1, start + 6] = r_2 + 2 * y_y0 * y_y0;
                        B[index + 1, start + 7] = 0;
                        B[index + 1, start + 8] = 0;

                        l[index, 0] = (xr + f * Xr / Zr);
                        l[index + 1, 0] = (yr + f * Yr / Zr);
                    }
                }
                #endregion



            }

            /// <summary>
            /// 更新外方元素的值
            /// </summary>
            /// <param name="X">改正数矩阵</param>
            private void UpdateData(Matrix X)
            {
                for (int i = 0, j = 0; i < outE.Count; i++, j = j + 6)
                {
                    outE[i].Spos.X = X[j, 0];
                    outE[i].Spos.Y = X[j + 1, 0];
                    outE[i].Spos.Z = X[j + 2, 0];
                    outE[i].phi = X[j + 3, 0];
                    outE[i].omega = X[j + 4, 0];
                    outE[i].kappa = X[j + 5, 0];
                }
                int start = 6 * outE.Count;
                inE.p0.X = X[start, 0];
                inE.p0.Y = X[start + 1, 0];
                inE.f = X[start + 2, 0];
                dParams.k1 = X[start + 3, 0];
                dParams.k2 = X[start + 4, 0];
                dParams.p1 = X[start + 5, 0];
                dParams.p2 = X[start + 6, 0];
                dParams.alph = X[start + 7, 0];
                dParams.beta = X[start + 8, 0];
            }

            #endregion



            #region 前方交会
            /// <summary>
            /// 前方交会
            /// </summary>
            /// <returns></returns>
            public List<OData> ForwardForcus()
            {
                List<List<_3D_Point>> points = new List<List<_3D_Point>>();
                for (int i = 0; i < outE.Count - 1; i++)
                {
                    points.Add(new List<_3D_Point>());
                    var R1 = outE[i].GetR();
                    var R2 = outE[i + 1].GetR();
                    List<_3D_Point> uvw1 = new List<_3D_Point>(),
                                    uvw2 = new List<_3D_Point>();
                    foreach (var ip in PassIList[i])
                    {
                        var xyf = new _3D_Point(ip.pos.X - inE.p0.X - Get_dx(ip), ip.pos.Y - inE.p0.Y - Get_dy(ip), -inE.f).ToColumnMatrix();
                        var uvw = R1 * xyf;
                        uvw1.Add(new _3D_Point(uvw[0, 0], uvw[1, 0], uvw[2, 0]));
                    }

                    foreach (var ip in PassIList[i + 1])
                    {
                        var xyf = new _3D_Point(ip.pos.X - inE.p0.X - Get_dx(ip), ip.pos.Y - inE.p0.Y - Get_dy(ip), -inE.f).ToColumnMatrix();
                        var uvw = R2 * xyf;
                        uvw2.Add(new _3D_Point(uvw[0, 0], uvw[1, 0], uvw[2, 0]));
                    }

                    double Xs2 = outE[i + 1].Spos.X, Xs1 = outE[i].Spos.X,
                           Ys2 = outE[i + 1].Spos.Y, Ys1 = outE[i].Spos.Y,
                           Zs2 = outE[i + 1].Spos.Z, Zs1 = outE[i].Spos.Z,
                           Bu = Xs2 - Xs1, Bv = Ys2 - Ys1, Bw = Zs2 - Zs1;

                    for (int j = 0; j < uvw1.Count; j++)
                    {
                        double w2 = uvw2[j].Z,
                               v2 = uvw2[j].Y,
                               u2 = uvw2[j].X,
                               u1 = uvw1[j].X,
                               v1 = uvw1[j].Y,
                               w1 = uvw1[j].Z,
                               N1 = (Bu * w2 - Bw * u2) / (u1 * w2 - u2 * w1),
                               N2 = (Bu * w1 - Bw * u1) / (u1 * w2 - u2 * w1);
                        var UVW1 = N1 * uvw1[j];
                        var UVW2 = N2 * uvw2[j];
                        double X = (Xs1 + Xs2 + UVW1.X + UVW2.X) / 2,
                               Y = (Ys1 + Ys2 + UVW1.Y + UVW2.Y) / 2,
                               Z = (Zs1 + Zs2 + UVW1.Z + UVW2.Z) / 2;
                        points[i].Add(new _3D_Point(X, Y, Z));
                    }
                }

                List<_3D_Point> pointList = new List<_3D_Point>();
                for (int i = 0; i < points[0].Count; i++)
                {
                    _3D_Point sum = new _3D_Point();
                    for (int j = 0; j < points.Count; j++)
                    {
                        sum += points[j][i];
                    }
                    pointList.Add(sum / points.Count);
                }

                //points[0].ForEach(p => p.Set_Accurate(6));
                //points[1].ForEach(p => p.Set_Accurate(6));
                //pointList.ForEach(p => p.Set_Accurate(6));


                //List<_3D_Point> tp = new List<_3D_Point>();
                //List<double> l = new List<double>();
                //StreamWriter sw = new StreamWriter("midvalue.txt"),
                //             sw1 = new StreamWriter("result.txt");
                //for (int i = 0; i < PassOList.Count; i++)
                //{
                //    l.Add(_3D_Point.Get_Norm(PassOList[i].pos, pointList[i]));
                //    l[i] = Methods.Set_Accurate(l[i], 6);
                //    sw.WriteLine(PassIList[0][i].Name + "," + points[0][i] + "," + points[1][i]);
                //    sw1.WriteLine(PassIList[0][i].Name + "," + pointList[i] + "," + l[i]);
                //}
                //sw1.Close();
                //sw.Close();
                for (int i = 0; i < pointList.Count; i++)
                {
                    PassOList.Add(new ObjectData { Name = PassIList[0][i].Name, pos = pointList[i] });
                }
                return PassOList;
            }

            private Matrix GetR(OutElement outElement)
            {
                DMath sin = Math.Sin, cos = Math.Cos;
                double[,] R = new double[3, 3];
                R[0, 0] = cos(outElement.phi) * cos(outElement.kappa) - sin(outElement.phi) * sin(outElement.omega) * sin(outElement.kappa);
                R[0, 1] = -cos(outElement.phi) * sin(outElement.kappa) - sin(outElement.phi) * sin(outElement.omega) * cos(outElement.kappa);
                R[0, 2] = -sin(outElement.phi) * cos(outElement.omega);
                R[1, 0] = cos(outElement.omega) * sin(outElement.kappa);
                R[1, 1] = cos(outElement.omega) * cos(outElement.kappa);
                R[1, 2] = -sin(outElement.omega);
                R[2, 0] = sin(outElement.phi) * cos(outElement.kappa) + cos(outElement.phi) * sin(outElement.omega) * sin(outElement.kappa);
                R[2, 1] = -sin(outElement.phi) * sin(outElement.kappa) + cos(outElement.phi) * sin(outElement.omega) * cos(outElement.kappa);
                R[2, 2] = cos(outElement.phi) * cos(outElement.omega);
                return new Matrix(R, 3, 3);
            }

            /// <summary>
            /// 获取delta_x
            /// </summary>
            /// <param name="id">像方坐标x，以相片中心为原点</param>
            /// <returns></returns>
            private double Get_dx(ImageData id)
            {
                double k1 = dParams.k1,
                      k2 = dParams.k2,
                      p1 = dParams.p1,
                      p2 = dParams.p2,
                      alph = dParams.alph,
                      beta = dParams.beta,
                      f = inE.f,
                      x0 = inE.p0.X,
                      y0 = inE.p0.Y,
                      r = _2D_Point.Get_Norm(inE.p0, id.pos),
                      r_2 = r * r,
                      r_4 = r_2 * r_2,
                      x_x0 = id.pos.X - x0,
                      y_y0 = id.pos.Y - y0;
                return x_x0 * (k1 * r_2 + k2 * r_4) + p1 * (r_2 + 2 * x_x0 * x_x0) + 2 * p2 * x_x0 * y_y0 + alph * x_x0 + beta * y_y0;
            }

            /// <summary>
            /// 获取delta_y
            /// </summary>
            /// <param name="id">像方坐标y，以相片中心为原点</param>
            /// <returns></returns>
            private double Get_dy(ImageData id)
            {
                double k1 = dParams.k1,
                      k2 = dParams.k2,
                      p1 = dParams.p1,
                      p2 = dParams.p2,
                      alph = dParams.alph,
                      beta = dParams.beta,
                      f = inE.f,
                      x0 = inE.p0.X,
                      y0 = inE.p0.Y,
                      r = _2D_Point.Get_Norm(inE.p0, id.pos),
                      r_2 = r * r,
                      r_4 = r_2 * r_2,
                      x_x0 = id.pos.X - x0,
                      y_y0 = id.pos.Y - y0;
                return y_y0 * (k1 * r_2 + k2 * r_4) + p2 * (r_2 + 2 * y_y0 * y_y0) + 2 * p1 * x_x0 * y_y0;
            }

            /// <summary>
            /// 严密的前方交会
            /// </summary>
            public List<OData> ForwardForcus(List<OData> originOdList)
            {
                Matrix t, x;
                List<_3D_Point> Xx = new List<_3D_Point>();
                //迭代求解加密点坐标(循环分块)
                do
                {
                    Xx.Clear();
                    List<Matrix> Bs = new List<Matrix>();
                    List<Matrix> ls = new List<Matrix>();
                    for (int i = 0; i < outE.Count; i++)
                    {
                        Bs.Add(GetFB(outE[i], PassIList[i], originOdList));
                        ls.Add(GetFl(outE[i], PassIList[i], originOdList));
                    }
                    Matrix B = new Matrix(Bs.Count * 2, 3);
                    Matrix lm = new Matrix(Bs.Count * 2, 1);
                    for (int m = 0; m < originOdList.Count; m++)
                    {
                        for (int i = 0; i < outE.Count; i++)
                        {
                            B[2 * i, 0] = Bs[i][2 * m, 0];
                            B[2 * i, 1] = Bs[i][2 * m, 1];
                            B[2 * i, 2] = Bs[i][2 * m, 2];
                            B[2 * i + 1, 0] = Bs[i][2 * m, 0];
                            B[2 * i + 1, 1] = Bs[i][2 * m + 1, 1];
                            B[2 * i + 1, 2] = Bs[i][2 * m + 1, 2];
                            lm[2 * i, 0] = ls[i][2 * m, 0];
                            lm[2 * i + 1, 0] = ls[i][2 * m + 1, 0];
                        }
                        x = (B.T() * B).Inverse() * B.T() * lm;
                        Xx.Add(new _3D_Point(x[0, 0], x[1, 0], x[2, 0]));

                    }
                    t = _3D_Point.ToColumnMatrix(Xx);
                    for (int i = 0; i < originOdList.Count; i++)
                    {
                        originOdList[i].pos += Xx[i];
                    }
                } while (!IsTerminating(0.000001, t));
                //StreamWriter sw = new StreamWriter("result.txt");
                //List<double> rs = new List<double>();

                //List<_3D_Point> ddxyz = new List<_3D_Point>();

                //for (int i = 0; i < ps.Count; i++)
                //{
                //    _3D_Point dxyz = ps[i].pos - PassOList[i].pos;
                //    ddxyz.Add(dxyz);
                //    double r = _3D_Point.Get_Norm(dxyz, new _3D_Point());
                //    rs.Add(r);
                //    sw.WriteLine(ps[i].Name + "," + ps[i].pos + "," + dxyz + "," + r);
                //}
                //double sum = 0;
                //rs.ForEach(r => sum += r * r); double a = Math.Sqrt(sum / rs.Count);
                //sw.WriteLine("dr=" + a + ",点位精度:" + a / 5000);
                //sw.Close();
                PassOList = originOdList;
                return PassOList;
            }

            /// <summary>
            /// 获取严密前方一张相片的系数矩阵
            /// </summary>
            /// <param name="oe"></param>
            /// <param name="id"></param>
            /// <param name="od"></param>
            /// <returns></returns>
            private Matrix GetFB(OutElement oe, List<ImageData> id, List<ObjectData> od)
            {
                Matrix B = new Matrix(2 * od.Count, 3);

                double f = inE.f,
                       x0 = inE.p0.X,
                       y0 = inE.p0.Y;

                Matrix R = oe.GetR();
                double a1 = R[0, 0],
                       a2 = R[0, 1],
                       a3 = R[0, 2],
                       b1 = R[1, 0],
                       b2 = R[1, 1],
                       b3 = R[1, 2],
                       c1 = R[2, 0],
                       c2 = R[2, 1],
                       c3 = R[2, 2];

                int j = 0;
                for (int i = 0; i < od.Count; i++)
                {
                    double Xr = a1 * (od[i].pos.X - oe.Spos.X)
                               + b1 * (od[i].pos.Y - oe.Spos.Y)
                               + c1 * (od[i].pos.Z - oe.Spos.Z),
                           Yr = a2 * (od[i].pos.X - oe.Spos.X)
                               + b2 * (od[i].pos.Y - oe.Spos.Y)
                               + c2 * (od[i].pos.Z - oe.Spos.Z),
                            Zr = a3 * (od[i].pos.X - oe.Spos.X)
                               + b3 * (od[i].pos.Y - oe.Spos.Y)
                               + c3 * (od[i].pos.Z - oe.Spos.Z),
                            x = id[i].pos.X - x0 - Get_dx(id[i]),
                            y = id[i].pos.Y - y0 - Get_dy(id[i]);

                    B[j, 0] = -(a1 * f + a3 * x) / Zr;
                    B[j, 1] = -(b1 * f + b3 * x) / Zr;
                    B[j, 2] = -(c1 * f + c3 * x) / Zr;


                    B[j + 1, 0] = -(a2 * f + a3 * y) / Zr;
                    B[j + 1, 1] = -(b2 * f + b3 * y) / Zr;
                    B[j + 1, 2] = -(c2 * f + c3 * y) / Zr;

                    j = j + 2;
                }
                return B;
            }

            /// <summary>
            /// 计算前方交会时的lxy
            /// </summary>
            /// <param name="oe">外方元素</param>
            /// <param name="od">物方坐标</param>
            /// <param name="id">像方坐标</param>
            /// <returns>l矩阵</returns>
            private Matrix GetFl(OutElement oe, List<ImageData> id, List<ObjectData> od)
            {
                double[] l = new double[od.Count * 2];
                int i = 0, k = 0;
                Matrix R =oe.GetR();
                double a1 = R[0, 0],
                       a2 = R[0, 1],
                       a3 = R[0, 2],
                       b1 = R[1, 0],
                       b2 = R[1, 1],
                       b3 = R[1, 2],
                       c1 = R[2, 0],
                       c2 = R[2, 1],
                       c3 = R[2, 2],
                       Xs = oe.Spos.X,
                       Ys = oe.Spos.Y,
                       Zs = oe.Spos.Z;
                foreach (var item in od)
                {
                    double x = id[k].pos.X - Get_dx(id[k]) - inE.p0.X,
                           y = id[k].pos.Y - Get_dy(id[k]) - inE.p0.Y;
                    l[i] = x + inE.f * (a1 * (item.pos.X - Xs)
                               + b1 * (item.pos.Y - Ys)
                               + c1 * (item.pos.Z - Zs)) /
                               (a3 * (item.pos.X - Xs)
                               + b3 * (item.pos.Y - Ys)
                               + c3 * (item.pos.Z - Zs));
                    l[i + 1] = y + inE.f * (a2 * (item.pos.X - Xs)
                               + b2 * (item.pos.Y - Ys)
                               + c2 * (item.pos.Z - Zs)) /
                               (a3 * (item.pos.X - Xs)
                               + b3 * (item.pos.Y - Ys)
                               + c3 * (item.pos.Z - Zs));
                    i = i + 2;
                    k++;
                }
                return new Matrix(l.GetLength(0), 1, l);
            }
            #endregion

            #region 光束法
            /// <summary>
            /// 光束法
            /// </summary>
            /// <param name="ie">内方位元素初值</param>
            /// <param name="oes">外方位元素初值</param>
            /// <param name="dp">畸变系数初值</param>
            /// <returns></returns>
            public List<OData> LightMethod(InElement ie,List<OutElement> oes,DParams dp)
            {
                SetOriginValue(ie, oes, dp);
                BackForcus();
                ForwardForcus(ForwardForcus());
                AllOList = MCOList.Concat(PassOList).ToList();
                AllIList =AllIList??new List<List<IData>>();
                for (int i = 0; i < MCIList.Count;++i )
                {
                    AllIList.Add(MCIList[i].Concat(PassIList[i]).ToList());
                }
                Matrix B = new Matrix(AllOList.Count * 2 * AllIList.Count, 6 * MCIList.Count),
                       C = new Matrix(B.Row, 3 * AllOList.Count),
                       l = new Matrix(B.Row, 1),
                       P = Matrix.Eye(B.Row);
#region 设置控制点的权值
                for (int i = 0; i < MCOList.Count;++i )
                {
                    for (int j = 0; j < outE.Count;++j )
                    {
                        P[2 * i + j * AllOList.Count*2, 2 * i + j * AllOList.Count*2] = 200;
                        P[2 * i + 1 + j * AllOList.Count*2, 2 * i + 1 + j * AllOList.Count*2] = 200;
                    }
                    
                }
#endregion
                double[] X = new double[6 * outE.Count];
                for (int i = 0, j = 0; i < outE.Count; i++, j = j + 6)
                {
                    X[j] = outE[i].Spos.X;
                    X[j + 1] = outE[i].Spos.Y;
                    X[j + 2] = outE[i].Spos.Z;
                    X[j + 3] = outE[i].phi;
                    X[j + 4] = outE[i].omega;
                    X[j + 5] = outE[i].kappa;
                }

                if (PassOList.Count == 0)
                {
                    return null;
                }
                //int start = 6 * outE.Count;
                //for (int i = 0; i < PassIList[0].Count; ++i)
                //{
                //    PassOList[i].pos.X = X[start + 3 * i];
                //    PassOList[i].pos.Y = X[start + 3 * i + 1];
                //    PassOList[i].pos.Z = X[start + 3 * i + 2];
                //}

                Matrix X0 = new Matrix(X.GetLength(0), 1, X);
                Matrix x;
                do
                {
                    SetBl_light1(B, C, l);
                    //var N11 = B.T() *  B;
                    //var N12 = B.T() *  C;
                    //var N21 = C.T() *  B;
                    //var arN22 = (C.T() *  C).Inverse();
                    //var M1 = B.T() *  l;
                    //var M2 = C.T() * l;
                    //x = (N11 - N12 * arN22 * N21).Inverse() * (M1 - N12 * arN22 * M2);
                    var AB = Matrix.ColumnCombine(B, C);
                    AB.OutPut("AB");
                    var y = (AB.T() * AB).Inverse() * AB.T() * l;
                    x = y.SubRMatrix(0, outE.Count * 6 - 1);
                    X0 = X0 + x;
                    UpdateData_light(X0);
                    ForwardForcus(PassOList);
                } while (!IsTerminating(0.000001, x));
                UpdateData_light(X0);
                //System.Windows.Forms.MessageBox.Show(X0.ToString());
                return PassOList;
            }
            private void SetBl_light(Matrix B,Matrix C,Matrix l)
            {
                int count = AllOList.Count;
                DMath sin = Math.Sin, cos = Math.Cos, tan = Math.Tan;
                double x0 = inE.p0.X,
                       y0 = inE.p0.Y,
                       f = inE.f,
                       k1 = dParams.k1,
                       k2 = dParams.k2,
                       p1 = dParams.p1,
                       p2 = dParams.p2,
                       alph = dParams.alph,
                       beta = dParams.beta;
                #region 设置B,l的值
                for (int i = 0; i < outE.Count; ++i)
                {
                    var R = outE[i].GetR();
                    double phi = outE[i].phi,
                           omega = outE[i].omega,
                           kappa = outE[i].kappa,
                           Xs = outE[i].Spos.X,
                           Ys = outE[i].Spos.Y,
                           Zs = outE[i].Spos.Z,
                           a1 = R[0, 0],
                           a2 = R[0, 1],
                           a3 = R[0, 2],
                           b1 = R[1, 0],
                           b2 = R[1, 1],
                           b3 = R[1, 2],
                           c1 = R[2, 0],
                           c2 = R[2, 1],
                           c3 = R[2, 2];
                    int indexj = i * 6;
                    for (int j = 0; j < count; ++j)
                    {
                        int index = i * count * 2 + 2 * j;


                        double X = AllOList[j].pos.X,
                               Y = AllOList[j].pos.Y,
                               Z = AllOList[j].pos.Z,
                               X_Xs = X - Xs,
                               Y_Ys = Y - Ys,
                               Z_Zs = Z - Zs,
                               Xr = a1 * X_Xs + b1 * Y_Ys + c1 * Z_Zs,
                               Yr = a2 * X_Xs + b2 * Y_Ys + c2 * Z_Zs,
                               Zr = a3 * X_Xs + b3 * Y_Ys + c3 * Z_Zs,
                               x = AllIList[i][j].pos.X,
                               y = AllIList[i][j].pos.Y,
                               x_x0 = x - x0,
                               y_y0 = y - y0,
                               r = _2D_Point.Get_Norm(AllIList[i][j].pos, inE.p0),
                               r_2 = r * r,
                               r_4 = r_2 * r_2,
                               dx = x_x0 * (k1 * r_2 + k2 * r_4) + p1 * (r_2 + 2 * x_x0 * x_x0) + 2 * p2 * x_x0 * y_y0 + alph * x_x0 + beta * y_y0,
                               dy = y_y0 * (k1 * r_2 + k2 * r_4) + p2 * (r_2 + 2 * y_y0 * y_y0) + 2 * p1 * x_x0 * y_y0,
                               xr = x_x0 - dx,
                               yr = y_y0 - dy;
                        B[index, indexj + 0] = (a1 * f + a3 * xr) / Zr;
                        B[index, indexj + 1] = (b1 * f + b3 * xr) / Zr;
                        B[index, indexj + 2] = (c1 * f + c3 * xr) / Zr;
                        B[index, indexj + 3] = y * sin(omega) - cos(omega) * (x * (x * cos(kappa) - y * sin(kappa)) / f + f * cos(kappa));
                        B[index, indexj + 4] = -f * sin(kappa) - x * (x * sin(kappa) + y * cos(kappa)) / f;
                        B[index, indexj + 5] = y;
                        C[index,  3 * j] = -B[index, indexj];
                        C[index,  3 * j + 1] = -B[index, indexj + 1];
                        C[index,  3 * j + 2] = -B[index, indexj + 2];

                        B[index + 1, indexj + 0] = (a2 * f + a3 * yr) / Zr;
                        B[index + 1, indexj + 1] = (b2 * f + b3 * yr) / Zr;
                        B[index + 1, indexj + 2] = (c2 * f + c3 * yr) / Zr;
                        B[index + 1, indexj + 3] = -x * sin(omega) - cos(omega) * (y * (x * cos(kappa) - y * sin(kappa)) / f - f * sin(kappa));
                        B[index + 1, indexj + 4] = -f * cos(kappa) - y * (x * sin(kappa) + y * cos(kappa)) / f;
                        B[index + 1, indexj + 5] = -x;
                        C[index + 1,3 * j] = -B[index + 1, indexj];
                        C[index + 1,3 * j + 1] = -B[index + 1, indexj + 1];
                        C[index + 1, 3 * j + 2] = -B[index + 1, indexj + 2];

                        l[index, 0] = (xr + f * Xr / Zr);
                        l[index + 1, 0] = (yr + f * Yr / Zr);
                    }
                }
                //B.OutPut("B");
                //C.OutPut("C");
                #endregion
            }

            private void SetBl_light1(Matrix B, Matrix C, Matrix l)
            {
                int count = AllOList.Count;
                DMath sin = Math.Sin, cos = Math.Cos;
                double x0 = inE.p0.X,
                       y0 = inE.p0.Y,
                       f = inE.f,
                       k1 = dParams.k1,
                       k2 = dParams.k2,
                       p1 = dParams.p1,
                       p2 = dParams.p2,
                       alph = dParams.alph,
                       beta = dParams.beta;
                #region 设置B,l的值
                for (int j = 0; j < count;++j )
                {
                    double X = AllOList[j].pos.X,
                               Y = AllOList[j].pos.Y,
                               Z = AllOList[j].pos.Z;

                    for (int i = 0; i < outE.Count; ++i)
                    {
                        int indexj = i * 6;
                        int index = j * outE.Count * 2 + 2 * i;
                        var R = outE[i].GetR();
                        double phi = outE[i].phi,
                               omega = outE[i].omega,
                               kappa = outE[i].kappa,
                               Xs = outE[i].Spos.X,
                               Ys = outE[i].Spos.Y,
                               Zs = outE[i].Spos.Z,
                               a1 = R[0, 0],
                               a2 = R[0, 1],
                               a3 = R[0, 2],
                               b1 = R[1, 0],
                               b2 = R[1, 1],
                               b3 = R[1, 2],
                               c1 = R[2, 0],
                               c2 = R[2, 1],
                               c3 = R[2, 2],
                               X_Xs = X - Xs,
                               Y_Ys = Y - Ys,
                               Z_Zs = Z - Zs,
                               Xr = a1 * X_Xs + b1 * Y_Ys + c1 * Z_Zs,
                               Yr = a2 * X_Xs + b2 * Y_Ys + c2 * Z_Zs,
                               Zr = a3 * X_Xs + b3 * Y_Ys + c3 * Z_Zs,
                               x = AllIList[i][j].pos.X,
                               y = AllIList[i][j].pos.Y,
                               x_x0 = x - x0,
                               y_y0 = y - y0,
                               r = _2D_Point.Get_Norm(AllIList[i][j].pos, inE.p0),
                               r_2 = r * r,
                               r_4 = r_2 * r_2,
                               dx = x_x0 * (k1 * r_2 + k2 * r_4) + p1 * (r_2 + 2 * x_x0 * x_x0) + 2 * p2 * x_x0 * y_y0 + alph * x_x0 + beta * y_y0,
                               dy = y_y0 * (k1 * r_2 + k2 * r_4) + p2 * (r_2 + 2 * y_y0 * y_y0) + 2 * p1 * x_x0 * y_y0,
                               xr = x_x0 - dx,
                               yr = y_y0 - dy;
                        //int indexj = i * 6;
                        B[index, indexj + 0] = (a1 * f + a3 * xr) / Zr;
                        B[index, indexj + 1] = (b1 * f + b3 * xr) / Zr;
                        B[index, indexj + 2] = (c1 * f + c3 * xr) / Zr;
                        B[index, indexj + 3] = y * sin(omega) - cos(omega) * (x * (x * cos(kappa) - y * sin(kappa)) / f + f * cos(kappa));
                        B[index, indexj + 4] = -f * sin(kappa) - x * (x * sin(kappa) + y * cos(kappa)) / f;
                        B[index, indexj + 5] = y;
                        C[index, 3 * j] = -B[index, indexj];
                        C[index, 3 * j + 1] = -B[index, indexj + 1];
                        C[index, 3 * j + 2] = -B[index, indexj + 2];

                        B[index + 1, indexj + 0] = (a2 * f + a3 * yr) / Zr;
                        B[index + 1, indexj + 1] = (b2 * f + b3 * yr) / Zr;
                        B[index + 1, indexj + 2] = (c2 * f + c3 * yr) / Zr;
                        B[index + 1, indexj + 3] = -x * sin(omega) - cos(omega) * (y * (x * cos(kappa) - y * sin(kappa)) / f - f * sin(kappa));
                        B[index + 1, indexj + 4] = -f * cos(kappa) - y * (x * sin(kappa) + y * cos(kappa)) / f;
                        B[index + 1, indexj + 5] = -x;
                        C[index + 1, 3 * j] = -B[index + 1, indexj];
                        C[index + 1, 3 * j + 1] = -B[index + 1, indexj + 1];
                        C[index + 1, 3 * j + 2] = -B[index + 1, indexj + 2];

                        l[index, 0] = (xr + f * Xr / Zr);
                        l[index + 1, 0] = (yr + f * Yr / Zr);
                    }
                }
                //B.OutPut("B");
                //C.OutPut("C");
                #endregion
            }
            private void UpdateData_light(Matrix X)
            {
                for (int i = 0, j = 0; i < outE.Count; i++, j = j + 6)
                {
                    outE[i].Spos.X = X[j, 0];
                    outE[i].Spos.Y = X[j + 1, 0];
                    outE[i].Spos.Z = X[j + 2, 0];
                    outE[i].phi = X[j + 3, 0];
                    outE[i].omega = X[j + 4, 0];
                    outE[i].kappa= X[j + 5, 0];
                }
            }
            #endregion

            /// <summary>
            /// 获得点位精度
            /// </summary>
            /// <param name="distance">摄影距离，单位与物方坐标相同</param>
            /// <returns></returns>
            public string GetAccuracy(double distance)
            {
                if (hasKnown)
                {
                    List<double> rs = new List<double>();

                    List<_3D_Point> ddxyz = new List<_3D_Point>();

                    for (int i = 0; i < PassOList.Count; i++)
                    {
                        _3D_Point dxyz = PassOList[i].pos - KnownOList[i].pos;
                        ddxyz.Add(dxyz);
                        double r = _3D_Point.Get_Norm(dxyz, new _3D_Point());
                        rs.Add(r);
                    }
                    double sum = 0;
                    rs.ForEach(r => sum += r * r);
                    double a = Math.Sqrt(sum / rs.Count);
                    return ("dr=" + a + ",点位精度:1/" + (distance / a).ToFormatString(0));
                }
                else
                    return string.Empty;
                
            }


            /// <summary>
            /// 根据给定的阈值判断是否终止迭代
            /// </summary>
            /// <param name="accurate"></param>
            /// <param name="x"></param>
            /// <returns></returns>
            private bool IsTerminating(double accurate, Matrix x)
            {
                for (int i = 0; i < x.Row; i++)
                    for (int j = 0; j < x.Column; j++)
                    {
                        if (x[i, j] <= accurate)
                            continue;
                        else
                            return false;
                    }
                return true;
            }

            /// <summary>
            /// 获取解算结果报告
            /// </summary>
            /// <returns></returns>
            public string GetReport()
            {
                StringBuilder sb = new StringBuilder("解算结果:".Endl());
                sb.AppendLine("控制点数量:" + cpointsCount);
                sb.AppendLine("重叠度:" + MCIList.Count);
                sb.AppendLine("加密点数量:" + PassIList[0].Count);
                sb.AppendLine("检核点数量:" + KnownOList.Count);
                sb.AppendLine("外方位元素:");
                sb.AppendLine("\tXs\tYs\tZs\tphi\tomega\tkappa");
                for (int i = 0; i < outE.Count; i++)
                {
                    var pos = outE[i].Spos.Clone();
                    //pos.Set_Accurate(6);
                    var opk = outE[i];
                    sb.AppendFormat("{0}\t{1}\t{2}\t", pos.X, pos.Y, pos.Z);
                    sb.AppendFormat("{0}\t{1}\t{2}", opk.phi, opk.omega, opk.kappa);
                    sb.AppendLine();
                    sb.AppendLine("旋转矩阵:");
                    sb.AppendLine( GetR(outE[i]).ToFormatString(6));
                }
                sb.AppendLine("内方位元素:");
                sb.AppendFormat("x0={0}\ty0={1}\tf={2}", inE.p0.X, inE.p0.Y, inE.f);
                sb.AppendLine();
                sb.AppendLine("畸变差系数:");
                sb.AppendFormat("k1={0:E}\tk2={1:E}\tp1={2:E}\tp2={3:E}\talph={4:E}\tbeta={5:E}",
                                dParams.k1, dParams.k2, dParams.p1, dParams.p2, dParams.alph, dParams.beta);
                sb.AppendLine();
                StringBuilder sb1 = new StringBuilder();
                sb1.AppendLine("加密点坐标:");
                sb1.AppendLine("点名\tX\tY\tZ");
                for (int i = 0; i < PassOList.Count; i++)
                {
                    var pos = PassOList[i].pos;
                    sb1.AppendFormat("{0}\t{1}\t{2}\t{3}\t", PassOList[i].Name, pos.X, pos.Y, pos.Z);
                    sb1.AppendLine();
                }
                sb1.AppendLine("检查点坐标差:");

                List<_3D_Point> dxyz = new List<_3D_Point>();
                List<double> dr = new List<double>();
                for (int i = 0; i < PassOList.Count; i++)
                {
                    var pos = PassOList[i].pos;
                    var rel = from p in KnownOList
                              where p.Name == PassOList[i].Name
                              select p.pos;
                    if (rel.Count() < 0)
                        continue;
                    double dx = pos.X - rel.First().X,
                           dy = pos.Y - rel.First().Y,
                           dz = pos.Z - rel.First().Z;
                    dxyz.Add(new _3D_Point(dx, dy, dz));
                    dr.Add(Sqrt(dx * dx + dy * dy + dz * dz));
                    sb1.AppendFormat("{0}\t{1}\t{2}\t{3}\t", PassOList[i].Name, dx, dy, dz);
                    sb1.AppendLine();
                }

                double RmsX, RmsY, RmsZ, RmsXY;
                double sumX = 0, sumY = 0, sumZ = 0, sumXY = 0;
                for (int i = 0; i < dxyz.Count; i++)
                {
                    sumX += dxyz[i].X * dxyz[i].X;
                    sumY += dxyz[i].Y * dxyz[i].Y;
                    sumZ += dxyz[i].Z * dxyz[i].Z;
                    sumXY += dxyz[i].X * dxyz[i].X + dxyz[i].Y * dxyz[i].Y;
                }
                RmsX = Sqrt(sumX / dxyz.Count);
                RmsY = Sqrt(sumY / dxyz.Count);
                RmsZ = Sqrt(sumZ / dxyz.Count);
                RmsXY = Sqrt(sumXY / dxyz.Count);
                sb.AppendLine("实际精度:");
                sb.AppendFormat("RMSX={0}\tRMSY={1}\tRMSZ={2}\tRMSXY={3}",
                                 RmsX, RmsY, RmsZ, RmsXY);
                sb.AppendLine();
                int index = dr.IndexOf(dr.Max());
                sb.AppendFormat("精度最弱点:{0},dr={1}", PassOList[index].Name, dr[index]);
                sb.AppendLine();
                
                sb.Append(sb1.ToString());
                return sb.ToString();
            }
        }
    }

}
