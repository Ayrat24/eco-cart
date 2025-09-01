// The following code runs operations against different large numbers.
// Demonstrated below is the ability to set and read values through the Inspector,
// as well as addition, multiplication and division operations.
// The demo also goes through the ways you can interact with the values and read the
// value components such as the coefficient and magnitude.

// Include this namespace to enable Large Numbers in your own code.
// using LargeNumbers;

using UnityEngine;
using UnityEngine.UI;
using System.Text;
using System.Collections;



// Option 1, define an extension method.
namespace LargeNumbers
{
    // 

}


namespace LargeNumbers.Example
{
    // This class demonstrates extending the number library with a ToCustomString extension method.
    public static class AlphabeticNotationExtension
    {
        public static string ToCustomString ( this AlphabeticNotation alp )
        {
            // Let's restrict to a max of two decial places.
            return $"{alp.coefficient:0.##}{AlphabeticNotation.GetAlphabeticMagnitudeName ( alp.magnitude )} [Extension Method 2 decimal places.]";
        }
    }



    public class LargeNumberTestBehaviour : MonoBehaviour
    {
        // The Large Number values can be serialised by Unity.
        [Header ( "Starting Values" )]
        public LargeNumber largeNumber = new LargeNumber ( 123.456, 123 );
        public ScientificNotation scientificNotation = new ScientificNotation ( 123.456, 123 );
        public AlphabeticNotation alphabeticNotation = new AlphabeticNotation ( 123.456, 123 );

        [ Space ( 10 )]
        public int iterations = 40;
        public double iterationMultiplication = 2_000_000;

        [Space(10)]
        [Header("UI Text Elements")]
        public Text IterationsText;
        public Text StartLargeNumberText;
        public Text LargeNumberText;
        public Text StartScientificNotationText;
        public Text ScientificNotationText;
        public Text StartAlphabeticNotationText;
        public Text AlphabeticNotationText;


        // This is yet another way to define a custom function. This method is demosntrated in the Start method below.
        private string ToStringFunction ( double coefficient, int magnitude )
        {
            // Let's restrict to a max of one decial place.
            return $"{coefficient:0.#}{AlphabeticNotation.GetAlphabeticMagnitudeName ( magnitude )} [Func 1 decimal place.]";
        }


        void Start ( )
        {
            // Let's start off with the most basic usage. This might be all you need to get started:
            var goldEarnt = new LargeNumber(123.456, 10);
            var goldMultiplier = 1.5f;

            Debug.Log ( $"<b><color=#ffffff>Basic Usage Example</color></b>\n\n" );


            Debug.Log ( $"goldEarnt * goldMultiplier:\n" +
                $"<color=#cccc00>{goldEarnt} * {goldMultiplier}</color> = <color=#ffff00>{goldEarnt * goldMultiplier}</color> (after multiplier)" );

            // Create a StringBuilder to create the log output
            var sb = new StringBuilder();
            {
                // Let's write out to the console the values that are held and viewed in the Inspector.

                // Calling ToString() is the easiest way to visualise the large number value.
                // It may not be the most efficient though, depending on your UI needs.
                sb.AppendLine ( "<color=white>1. Printing out the values from the Inspector. (Click here to see the results)</color>\n" );
                sb.AppendLine ( $"Printing the deserialised LargeNumber value as a string : {largeNumber}." );
                sb.AppendLine ( $"  coefficient : {largeNumber.coefficient}. \n  magnitude : {largeNumber.magnitude}." );
                sb.AppendLine ( $"  large number name : {LargeNumber.GetLargeNumberName ( largeNumber.magnitude )}\n" );

                sb.AppendLine ( $"Printing ScientificNotation from Inspector : {scientificNotation}.\n  {scientificNotation.ToValuesString ( )}.\n" );
                sb.AppendLine ( $"Printing AlphabeticNotation from Inspector : {alphabeticNotation}.\n  {alphabeticNotation.ToValuesString ( )}.\n" );

                Debug.Log ( sb.ToString ( ) );
            }


            {
                sb.Clear ( );
                sb.AppendLine ( "<color=white>2. Multiplying large numbers by themselves. (Click here to see the results)</color>\n" );
                // Now we can run some mathematic operations on some new values.

                // We set up some simple Double types here for testing.
                var d1 = 1d;
                var d2 = 111_000_000_000d;

                // We create two LargeNumber items from the above double values. This shows one way to create a new LargeNumber.
                var l1 = new LargeNumber ( d1 );
                var l2 = new LargeNumber ( d2 );


                _ = new LargeNumber ( ); // we can also just create a new default LargeNumber with a value of 0 (coefficient and magnitude).

                // We create two ScientificNotation items from the above double values.
                var s1 = new ScientificNotation ( d1 );
                var s2 = new ScientificNotation ( d2 );

                var a1 = new AlphabeticNotation ( d1 );
                var a2 = new AlphabeticNotation ( d2 );

                // We could even create new ScientificNotation or AlphabeticNotation numbers from previous other large number types.
                var newS2FromL2 = (ScientificNotation) l2;
                var newA2FromL2 = (AlphabeticNotation) l2;

                // The following two loops are just testing some multiplication and division of LargeNumber and ScientificNotation values.
                // At the end of the following two loops, the final results should be very close to the original. Unfortunately due to floating
                // point drift, there might be very small differences in the final number, but that's the nature of FPNs. 
                // See https://en.wikipedia.org/wiki/Floating-point_arithmetic for more details on FPNs.

                for ( int n = 0; n < 5; n++ )
                {
                    sb.Append ( $"[{n}] Operation {d1} * {d2} = " );
                    d1 *= d2; // multiply two double values
                    l1 *= l2; // multiply two LarneNumber values
                    s1 *= s2; // multiply two ScientificNotation values
                    a1 *= a2; // multiply two AlphabeticNotation values

                    // Now format the results to display in the Console window.
                    sb.AppendLine ( $"{d1}." );
                    sb.AppendLine ( $" LargeNumber = \t\t{l1}. {l1.ToValuesString ( )} double = {( double ) l1}" );
                    sb.AppendLine ( $" ScientificNotation = \t{s1}. {s1.ToValuesString ( )} double = {( double ) s1}" );
                    sb.AppendLine ( $" AlphabeticNotation = \t{a1}. {a1.ToValuesString ( )} double = {( double ) a1}\n" );
                }
                Debug.Log ( sb.ToString ( ) );

                sb.Clear ( );
                sb.AppendLine ( "<color=white>3. Divide the numbers to end up with the start values, where we MIGHT see small the floating point drift.\n(Click here to see the results)</color>\n" );
                for ( int n = 0; n < 5; n++ )
                {
                    sb.AppendLine ( $"[{n}]\n Standard Double Operation {d1} / {d2} = " );
                    d1 /= d2; // divide two double values
                    l1 /= l2; // divide two LarneNumber values
                    s1 /= s2; // divide two ScientificNotation values
                    a1 /= a2; // divide two AlphabeticNotation values

                    // Now format the results to display in the Console window.
                    sb.AppendLine ( $"{d1}." );
                    sb.AppendLine ( $" LargeNumber = \t\t{l1}. {l1.ToValuesString ( )} double = {( double ) l1}" );
                    sb.AppendLine ( $" ScientificNotation = \t{s1}. {s1.ToValuesString ( )} double = {( double ) s1}" );
                    sb.AppendLine ( $" AlphabeticNotation = \t{a1}. {a1.ToValuesString ( )} double = {( double ) a1}\n" );
                }
                Debug.Log ( sb.ToString ( ) );
            }


            {
                sb.Clear ( );
                sb.AppendLine ( "<color=white>4. Casting values from one to another. (Click here to see the results)</color>\n" );

                // The following code demonstrates implicitly casting from one large number to another.

                LargeNumber l1 = new LargeNumber ( 123.456, 20 ); // = 1.23456 x 10^6
                ScientificNotation s1 = new ScientificNotation ( 123.456, 60 ); // = 1.23456 x 10^6
                AlphabeticNotation a1 = new AlphabeticNotation ( 123.456, 20 ); // = 1.23456 x 10^6

                LargeNumber l2 = s1; // implicitly cast a ScientificNotation to a LargeNumber
                ScientificNotation s2 = a1; // implicitly cast a AlphabeticNotation to a ScientificNotation
                AlphabeticNotation a2 = l1; // implicitly cast a LargeNumber to a AlphabeticNotation

                var l3 = ( LargeNumber ) a2;// explicitly cast a AlphabeticNotation to a LargeNumber
                var s3 = ( ScientificNotation ) l2; // explicitly cast a LargeNumber to a ScientificNotation
                var a3 = ( AlphabeticNotation ) s2; // explicitly cast a ScientificNotation to a AlphabeticNotation

                sb.AppendLine ( $"ScientificNotation : {s1} {s1.ToValuesString ( )}\n" +
                    $" -> LargeNumber : {l2} {l2.ToValuesString ( )} \n" +
                    $" -> ScientificNotation : {s3} {s3.ToValuesString ( )}\n" );

                sb.AppendLine ( $"AlphabeticNotation : {a1} {a1.ToValuesString ( )}" +
                    $" -> ScientificNotation : {s2} {s2.ToValuesString ( )} \n" +
                    $" -> AlphabeticNotation : {a3} {a3.ToValuesString ( )}\n" );

                sb.AppendLine ( $"LargeNumber : {l1} {l1.ToValuesString ( )}" +
                    $" -> AlphabeticNotation : {a2} {a2.ToValuesString ( )} \n" +
                    $" -> LargeNumber : {l3} {l3.ToValuesString ( )}\n" );

                Debug.Log ( sb.ToString ( ) );
            }

            // This test demonstrates comparing large number values against each other. Works with all large number types.
            {
                sb.Clear ( );
                sb.AppendLine ( "<color=white>5. Comparing some LargeNumber values against each other and zero. (Click here to see the results)</color>\n" );
                var a = new LargeNumber ( 0.1, 105 );
                var b = new LargeNumber ( 20, 105 );
                var c = new LargeNumber ( 0.1, 106 );

                sb.AppendLine ( $"{a} < {b} = {a < b}" );
                sb.AppendLine ( $"{a} < {c} = {a < c}" );
                sb.AppendLine ( $"{b} < {c} = {b < c}" );

                sb.AppendLine ( $"{a} < 0 = {a < LargeNumber.zero}" );
                sb.AppendLine ( $"{b} > 0 = {b > LargeNumber.zero}" );
                sb.AppendLine ( $"{c} == 0 = {c == LargeNumber.zero}" );

                var d = new LargeNumber ( -0.1, -333 );
                sb.AppendLine ( $"{d} < 0 = {d < LargeNumber.zero}" );

                Debug.Log ( sb.ToString ( ) );
            }

            {
                string [ ] tests = { "M", "T", "a", "z", "aa", "az", "ba", "zz", "aaa", "xyz", "zzz", "aza", "azb", "aaaa", "zzzzzz", "aaaaaaa", "aazaa", "aazzz", "abaaa", "zaaaaa", "fxshrxr", "fxshrxs", "fxshrxt", "Kᵗʰ", "aᵗʰ", "bᵗʰ", "fxshrxsᵗʰ", "fxshrxtᵗʰ" };

                sb.Clear ( );
                sb.AppendLine ( "<color=white>6. Getting some AlphabeticNotation magnitudes from a string (and back again). (Click here to see the results)</color>\n" );
                for ( int i = 0, count = tests.Length; i < count; ++i )
                {
                    if ( !AlphabeticNotation.GetMagnitudeFromAlphabeticName ( tests [ i ], out var mag ) )
                        sb.AppendLine ( $"  6.{i} Could not get magnitude from '{tests [ i ]}' [{mag}]" );
                    else sb.AppendLine ( $"  6.{i} '{tests [ i ]}' => {mag} => {AlphabeticNotation.GetAlphabeticMagnitudeName ( mag )}" );
                }

                Debug.Log ( sb.ToString ( ) );
            }

            // The following test will 'Parse' some strings into an AlphabeticNotation value if possible. Don't be surpirsed to see lots of floating point errors here.
            {
                string [ ] tests = { "123", "1234567890", "123.456 abc", "-123,456xyz", "98.76 Kᵗʰ", ".1B", "abcd", "1.23zzz", "1.23aaaa", "1.23yzzz", "1.23zaaa", "9.876 fxshrxs" };

                sb.Clear ( );
                sb.AppendLine ( "<color=white>7. Getting some AlphabeticNotation items from a string. (Click here to see the results)</color>\n" );
                for ( int i = 0, count = tests.Length; i < count; ++i )
                {
                    if ( !AlphabeticNotation.GetAlphabeticNotationFromString ( tests [ i ], out var alphabeticNotation ) )
                        sb.AppendLine ( $"  7.{i} Could not get AlphabeticNotation from '{tests [ i ]}'" );
                    else sb.AppendLine ( $"  7.{i} '{tests [ i ]}' => {alphabeticNotation} {alphabeticNotation.ToValuesString ( )}" );
                }

                Debug.Log ( sb.ToString ( ) );
            }

            {
                sb.Clear ( );
                sb.AppendLine ( "<color=white>8. Comparing large and small numbers (Click here to see the results)</color>\n" );
                var large = new AlphabeticNotation ( 123, 1234567890 );
                var small = new AlphabeticNotation ( 123, 1 );
                sb.AppendLine ( $"small {small.ToValuesString ( )}. large {large.ToValuesString ( )}" );
                sb.AppendLine ( $"large <= small {large <= small}.\nsmall <= large {small <= large}" );

                Debug.Log ( sb.ToString ( ) );
            }

            {
                // Here we assign a new Func to the AlphabeticNotation struct at a static level, which will be run instead of the previous ToString code. 
                AlphabeticNotation.ToStringFunction = ToStringFunction;
                var a1 = new AlphabeticNotation ( 123.456f, 78 );

                sb.Clear ( );
                sb.AppendLine ( "<color=white>9. Comparing ways to display custom string (Click here to see the results)</color>\n" );
                sb.AppendLine ( $"AlphabeticNotation ToString: {a1.ToString ( )}" );
                sb.AppendLine ( $"AlphabeticNotation ToCustomString: {a1.ToCustomString ( )}" );
                Debug.Log ( sb.ToString ( ) );
            }

            Debug.Log ( " ... Now on to the GUI iteration part of the demonstration..\n\n" );
            StartCoroutine ( Iterate ( ) );
        }


        /// <summary>
        /// The Iterate coroutine here will drive the GUI part of the demonstration. It will run the number of iterations as specified in the Inspector.
        /// </summary>
        private IEnumerator Iterate ( )
        {
            StartLargeNumberText.text = largeNumber.ToString ( );
            StartScientificNotationText.text = scientificNotation.ToString ( );
            StartAlphabeticNotationText.text = alphabeticNotation.ToString ( );

            IterationsText.text = $"Iteration : 0/{iterations}.";

            for ( var iterationCount = 0; iterationCount < iterations; iterationCount++ )
            {
                yield return new WaitForSeconds ( 0.5f );

                IterationsText.text = $"Iteration : {iterationCount+1}/{iterations}.";

                // You'll also notice that because we're working with the public values, the Inspector will also show the modified values during Play.
                // This only occurs during runtime, so when the editor stops playing, the values are returned to the original values.

                largeNumber *= iterationMultiplication; // multiply a LargeNumber value by a double.
                scientificNotation *= iterationMultiplication; // multiply a ScientificNotation value by double.
                alphabeticNotation *= iterationMultiplication; // multiply a AlphabeticNotation value by double.

                LargeNumberText.text = largeNumber.ToString ( );
                ScientificNotationText.text = scientificNotation.ToString ( );
                AlphabeticNotationText.text = alphabeticNotation.ToString ( );

            }

            IterationsText.text = $"Iteration : {iterations}/{iterations}. Finished.";
            Debug.Log ( $"Finish. Final Values as Standard Double values. (Click here to see the results) \n\n" +
                $"largeNumber \t\t{largeNumber.Standard ( )}.\n" +
                $"scientificNotation \t{scientificNotation.Standard ( )}.\n" +
                $"alphabeticNotation \t {alphabeticNotation.Standard ( )}" );
        }
    }
}