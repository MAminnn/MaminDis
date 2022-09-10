// random-noise-processor.js
let now = Date.now();
let counter = 0;

class MyAudioProcessor extends AudioWorkletProcessor {

    
    process(inputs, outputs, parameters) {
        console.log(outputs);
        console.log(parameters);
        return true;
    }
}

registerProcessor('my-audio-processor', MyAudioProcessor)



//counter++;
//if (Date.now() - now > 1000) {
//    console.log(counter)
//    counter = 0
//    now = Date.now()
//}
//return true;