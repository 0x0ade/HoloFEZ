using UnityEngine;
using System;
using System.Collections.Generic;
using FezEngine.Structure;
using FmbLib;
using System.IO;

public interface IFillable<A> { void Fill(A a); }
public interface IFillable<A,B> { void Fill(A a, B b); }
public interface IFillable<A,B,C> { void Fill(A a, B b, C c); }
public interface IFillable<A,B,C,D> { void Fill(A a, B b, C c, D d); }
public interface IFillable<A,B,C,D,E> { void Fill(A a, B b, C c, D d, E e); }
public interface IFillable<A,B,C,D,E,F> { void Fill(A a, B b, C c, D d, E e, F f); }
