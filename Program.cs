//test
using System;
using System.IO;
using System.Linq;
using System.Numerics;

namespace Bird;

class Program
{

    static void Main(string[] args)
    {
        float step=0.1f;

        string argumentsPath = Path.Combine("Arguments");
        string resultsPath = Path.Combine("Results"); 

        float[] numbers = File.ReadAllLines(argumentsPath)
                             .Select(line => float.Parse(line))
                             .ToArray();

        Shape border = new Shape();
        border.AppendLine(new Vector2(0, 0),new Vector2(200,100));
        //border.AppendLine(new Vector2(-30000, 0),new Vector2(30000,0));
        //border.AppendLine(new Vector2(0, 0),new Vector2(200,10));
        border.Box(new Vector2(100,100), 200);
        //Projectile bird = new Projectile(numbers[0], numbers[1]);
        Ball bird = new Ball(new Vector2(numbers[0], numbers[1]), Vector2.Zero);
        float tmax = bird.MomentLanding(numbers[2], numbers[3])*4;

        Vector2 p = bird.Throw(numbers[2], numbers[3],  0);
        bird.Force_Angle(numbers[2], numbers[3]);
        string ResultLine="";
        File.WriteAllText(resultsPath, ResultLine);

        for (float i =  0; i < tmax; i += step) 
        {

            List<Collision> colls = border.DetectCollision(bird); 
           
            string s = "";
            foreach(Collision coll in colls){
                    Vector2 n = coll.Line.Normal;
                    
                    // Проверяем направление вектора скорости относительно нормали
                    if (Vector2.Dot(bird.Velocity, n) > 0)
                    {
                        // Если вектор скорости направлен от поверхности, используем обратное направление нормали
                        n = -n;
                    }

                    // Применяем отскок к скорости снаряда
                    float e = 1.8f; // коэффициент упругости
                    Vector2 r = -e * Vector2.Dot(bird.Velocity, n) * n;
                    if(bird is Ball){
                        bird.Position=coll.Position + n*(bird.radius+float.Epsilon*10);
                        //Console.WriteLine(n*(bird.radius)-bird.Velocity*step/100);
                    }
                    else if(bird is Projectile){
                        bird.Position=coll.Position-bird.Velocity*(step/100);
                       
                    }
                    
                    bird.Force_Vec(r);
                    //Console.WriteLine($"Coll {coll.Value.Position}   {n*2}");
                    s = $"    Coll: {coll.Position} {colls.Count()} with [{coll.Line.A}; {coll.Line.B}]. Set Pos: {bird.Position} N:{n}";
            }
                ResultLine = $"Moment: {i:F3}    X: {bird.Position.X:F9}    Y: {bird.Position.Y:F9}" + s + '\n';
                Console.Write(ResultLine);
                File.AppendAllText(resultsPath, ResultLine);
                bird.Diff(step);
        }
    }
}
/*
public interface ICollider
{
    bool DetectCollision(ICollider other);
}*/



public class LineSegment{
    public Vector2 A { get; set; }
    public Vector2 B { get; set; }
    public float Length {get; set; }
    public Vector2 Normal {get; set; }

    public LineSegment(Vector2 A, Vector2 B){
        this.A=A;
        this.B=B;
        Length=Vector2.Distance(B,A);

        Vector2 C = A-B;
        Normal= new Vector2(C.Y, C.X)/Length;
    }

    public Vector2 DirectionVec(){
        return (A-B)/Length;
    }

    public Vector2 NormalCalculation(){
        Vector2 C = A-B;
        Normal= new Vector2(C.Y, C.X)/C.Length();
        return Normal;
    }

    public bool InLineSegment(Vector2 P){

        float Tx=(P.X-B.X)/(A.X-B.X);
        float Ty=(P.Y-B.Y)/(A.Y-B.Y);
        if(float.IsNaN(Tx) && float.IsNaN(Ty)){
            return false;
        }
        else if (float.IsNaN(Tx)){
            return 0<=Ty && Ty<=1;
        }
        else if (float.IsNaN(Ty)){
            return 0<=Tx && Tx<=1;
        }
        else if (Math.Abs(Tx - Ty) < 0.1){
            Console.WriteLine($"{Tx}    {Ty}");
            return (0<=Tx && Tx<=1) && (0<=Ty && Ty<=1);
        }
        return false; 
    }

    public Vector2? CrossLine(LineSegment Line){
        float CrossX = ((Line.A.X*Line.B.Y - Line.A.Y*Line.B.X) * (this.A.X - this.B.X) - (this.A.X*this.B.Y - this.A.Y*this.B.X)*(Line.A.X - Line.B.X))/
            ((Line.A.X-Line.B.X)*(this.A.Y-this.B.Y) - (Line.A.Y-Line.B.Y)*(this.A.X-this.B.X));
        float CrossY = ((Line.A.X*Line.B.Y - Line.A.Y*Line.B.X) * (this.A.Y - this.B.Y) - (this.A.X*this.B.Y - this.A.Y*this.B.X)*(Line.A.Y - Line.B.Y))/
            ((Line.A.X-Line.B.X)*(this.A.Y-this.B.Y) - (Line.A.Y-Line.B.Y)*(this.A.X-this.B.X));
        Vector2 C = new Vector2(CrossX, CrossY);

        if (this.InLineSegment(C)){
            return C;
        }
        else{
            return null;
        }
    }

}

public struct Collision{
    public bool State {get; set;}
    public Vector2 Position {get; set;}
    public LineSegment Line {get; set;}
    public Collision(bool state, Vector2 position, LineSegment line)
    {
        State = state;
        Position = position;
        Line = line;
    }
}
public class Shape{
    public List<LineSegment> Lines { get; set; }
    public int QuantityLines;
    

    public delegate void CollisionEventHandler(Collision collision);
    public event CollisionEventHandler CollisionDetected;

    public Shape(){
        Lines = new List<LineSegment>();
        QuantityLines=Lines.Count;
    }

    public void Box(Vector2 Position, float a)
    {
        Shape P = new Shape();

        Vector2 A = new Vector2(Position.X-a/2f, Position.Y-a/2f);
        Vector2 B = new Vector2(Position.X-a/2f, Position.Y+a/2f);
        Vector2 C = new Vector2(Position.X+a/2f, Position.Y+a/2f);
        Vector2 D = new Vector2(Position.X+a/2f, Position.Y-a/2f);

        this.AppendLine(A,B);
        this.AppendLine(B,C);
        this.AppendLine(C,D);
        this.AppendLine(D,A);
    }

    public List<Collision> DetectCollision(Projectile projectile)
    {   
        List<Collision> Colls = new List<Collision>();
        foreach (var line in Lines)
        { 

            LineSegment Trace = new LineSegment(projectile.Position, projectile.Position+projectile.Velocity);
            Vector2? P = line.CrossLine(Trace);
            if (P.HasValue && (P.Value - projectile.Position).LengthSquared() < projectile.VelocityE)
            {
                Collision collision=new Collision(true, P.Value, line); 
                CollisionDetected?.Invoke(collision);
                Colls.Add(collision);
            }

        }

        return Colls; // Пересечение не обнаружено
    }

    public List<Collision> DetectCollision(Ball projectile)
    {   
        List<Collision> Colls = new List<Collision>();
        foreach (var line in Lines){
           
            LineSegment Perpendicular = new LineSegment(projectile.Position, projectile.Position+line.Normal);
            LineSegment Hypotenuse = new LineSegment(projectile.Position, projectile.Position+projectile.Velocity);
            Vector2? A = line.CrossLine(Hypotenuse);
            Vector2? Projection = line.CrossLine(Perpendicular);
            
            if(Projection.HasValue && A.HasValue){ 
                float E = (float)Math.Pow(projectile.radius,2)+projectile.VelocityE;
                if ( (Projection.Value - projectile.Position).LengthSquared() <=  E){
                    
                    if ( (A.Value-projectile.Position+projectile.Velocity).LengthSquared()>(A.Value-projectile.Position).LengthSquared() && (A.Value-projectile.Position).LengthSquared() <= E){
                        Collision collision=new Collision(true, Projection.Value, line); 
                        CollisionDetected?.Invoke(collision);
                        Colls.Add(collision);// Пересечение обнаружено
                    }   
                }
            }
        }

        return Colls; 
    }


    public void AppendLine(Vector2 a, Vector2 b){
        Lines.Add(new LineSegment(a, b));
        QuantityLines=Lines.Count;
    }
}

public static class World{
    public static Vector2 G { get; set; } = new Vector2(0f, -9.80665f);
    public static Vector2 MoveMedium { get; set; } = new Vector2(0f, 0f);
    public static float density { get; set; } = 1.2255f;
}

public class Projectile{

    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; set; }
    public float VelocityE = float.Epsilon;

    public float mass {get; set;}
    public Projectile(float x=0, float y=0, float vx=0, float vy=0, float mass = 1f)
    {
        this.Position = new Vector2(x, y); 
        this.Velocity= new Vector2(vx, vy);
        this.mass = mass;
    }
    public Projectile(Vector2 Position, Vector2 Velocity, float mass = 1f)
    {
        this.Position = Position; 
        this.Velocity = Velocity;
        this.mass = 1f;
    }
    public void SetVelocity(float a){
        this.Velocity=this.Velocity/this.Velocity.Length()*a;
    }
    public void SetVelocityVec(Vector2 a){
        this.Velocity=a;
    }
    public static Projectile Projectile_Vec(Vector2 Position, Vector2 Velocity)
    {
        return new Projectile(Position.X, Position.Y, Velocity.X, Velocity.Y);
    }
    public static Projectile Projectile_Angle(Vector2 Position, float angle, float force)
    {
        float rad = (angle /  180.0f) * (float)Math.PI;
        float vx  = (float)Math.Cos(rad) * force;
        float vy  = (float)Math.Sin(rad) * force;
        return new Projectile(Position.X, Position.Y, vx, vy);
    }
    public void Force_Vec(Vector2 ForceV){
        Velocity=Velocity+ForceV;
    }
    public void Force_Angle(float force, float angle){
        float rad = (angle /  180.0f) * (float)Math.PI;
        float vx  = (float)Math.Cos(rad) * force;
        float vy  = (float)Math.Sin(rad) * force;

        Velocity=Velocity + new Vector2(vx,vy);
    }

    public float MomentLanding(float force0, float angle)
    {
        float rad = (angle /  180.0f) * (float)Math.PI;
        float tmax = (2.0f * force0 * (float)Math.Sin(rad)) / (-World.G.Y);
        return tmax;
    }

    public Vector2 Throw(float force0, float angle, float moment)
    {
        float rad = (angle /  180.0f) * (float)Math.PI;
        float x = Position.X + force0 * (float)Math.Cos(rad) * moment;
        float y = Position.Y + force0 * (float)Math.Sin(rad) * moment + (World.G.Y * (moment * moment)) /  2.0f;
        return new Vector2(x, y); 
    }
    public virtual void Diff(float Diff_t){

        Position=Position+Velocity*Diff_t;
        Force_Vec(World.G);
        VelocityE=Velocity.Length()*Diff_t*2;
    }
}

public class Ball:Projectile{
    public float radius { get; set; } = 0.1f;
    public float midsection { get; set; } = (float)Math.PI*0.01f;

    public Ball(Vector2 Position, Vector2 Velocity, float radius=0.1f, float mass=1f){
        this.Position=Position;
        this.radius=radius;
        this.mass=mass;
        this.Velocity=Velocity;
        this.midsection=(float)Math.PI*radius*radius;
        this.VelocityE=float.Epsilon;
    }

    public override void Diff(float Diff_t){
        Position=Position+Velocity*Diff_t;
        Vector2 VelocityInMedium = (Velocity-World.MoveMedium)*Diff_t;
        float l = VelocityInMedium.Length();
        Vector2 Force = -(VelocityInMedium*l*World.density)/2 * midsection;
       
        Force_Vec(World.G * Diff_t + Force/mass);
        VelocityE=Velocity.Length()*Diff_t*2;
    }

    
}