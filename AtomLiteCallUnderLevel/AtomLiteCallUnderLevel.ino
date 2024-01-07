/*
ボード:ESP32 PICO Kit
UploadSpeed:115200
*/
#include <BLEDevice.h>
#include <BLEServer.h>
#include <BLEUtils.h>
#include <BLE2902.h>
#include "M5Atom.h"


#define BLE_PASSKEY 123456 //ペアリング時のパスコード値設定
#define DEVICE_NAME "ESP32PIRTRI" //Bluetoothデバイス名設定

#define RED_LED   19  //赤色LEDのGPIO番号を指定
#define GREEN_LED 23  //緑色LEDのGPIO番号を指定
#define BLUE_LED  33  //青色LEDのGPIO番号を指定
#define PIR_PIN   21  //PIR出力PINを指定
#define PUSH_BUTTON_PIN 25 //押しボタンのPINを指定
#define BUTTON_LED_PIN 21 //押しボタンのLEDを指定
 
#define IS_GPIO_POS false //GPIO出力を正論理でやるか否か

#define BUZZER_PIN 22
//音を鳴らす時間
#define BEAT 500
//音階名と周波数の対応
#define C4 261.6
#define A4 440
#define F4 349.228
#define C5 523.251

union LedStatus{
  struct {
    unsigned char red:1;
    unsigned char green:1;
    unsigned char blue:1;
  }b;
  unsigned char UCHAR_8;
};

union LedStatus currentLedStatus;
union LedStatus oldLedStatus;

BLEServer* pServer = NULL;
BLECharacteristic* pCharacteristic = NULL;

bool ledOnOff=true;
bool beforeStateSwitch=LOW;
bool deviceConnected = false;
bool oldDeviceConnected = false;
uint32_t value = 0;
hw_timer_t * tim0 = NULL;
uint16_t gpioLEDStatus=0;
bool stopSound=false;
bool stopSoundOld=true;

//参考Url_Bluetooth：https://hawksnowlog.blogspot.com/2021/09/esp32-implementes-notify-services.html
//参考Url_Timer割り込み：https://lang-ship.com/blog/work/esp32-timer/
// See the following for generating UUIDs:
// https://www.uuidgenerator.net/

#define SERVICE_UUID        "e72609f6-2bcb-4fb0-824a-5276ec9e355d"
#define CHARACTERISTIC_UUID "cca99442-dab6-4f69-8bc2-685e2412d178"

// FastLEDライブラリの設定（CRGB構造体）
CRGB dispColor(uint8_t r, uint8_t g, uint8_t b) {
  return (CRGB)((r << 16) | (g << 8) | b);
}

enum statusBlinkMode{
  on,
  off
};

enum statusBLEState{
  connect,
  disconnect,
  ASAP,
  WAIT,
  WRONG,
  EMERGENCY,
  GOOD,
  CALL,
  none,
};

volatile statusBlinkMode statusBlink;
volatile statusBLEState bleState=disconnect;
 
class MyServerCallbacks: public BLEServerCallbacks {
    void onConnect(BLEServer* pServer) {
      deviceConnected = true;
      bleState=connect;
    };

    void onDisconnect(BLEServer* pServer) {
      deviceConnected = false;
      bleState=disconnect;
    }
};

class MyCallbacks: public BLECharacteristicCallbacks {
    void onWrite(BLECharacteristic *pCharacteristic) {
      std::string value = pCharacteristic->getValue();
      String strValue=value.c_str();

      if (strValue.length() > 0) {
        if(strValue.equals("a")){
            Serial.println("ASAP");
            bleState=ASAP;
        }else if(strValue.equals("b")){
            Serial.println("WAIT");
            bleState=WAIT;
        }else if(strValue.equals("c")){
            Serial.println("WRONG");
            bleState=WRONG;
        }else if(strValue.equals("d")){
            Serial.println("CANCEL");
            bleState=connect;
        }else if(strValue.equals("e")){
            Serial.println("EMERGENCY");
            bleState=EMERGENCY;
        }else{
            bleState=none;
        }
        Serial.println("*********");
        Serial.print("New value: ");
        for (int i = 0; i < value.length(); i++)
          Serial.print(value[i]);

        Serial.println();
        Serial.println("*********");
      }
      
    }
};

// ペアリング処理用
//参考Url：https://qiita.com/poruruba/items/eff3fedb1d4a63cbe08d
class MySecurity : public BLESecurityCallbacks {
  bool onConfirmPIN(uint32_t pin){
    return false;
  }

  uint32_t onPassKeyRequest(){
    Serial.println("ONPassKeyRequest");
    return BLE_PASSKEY;
  }

  void onPassKeyNotify(uint32_t pass_key){
    // ペアリング時のPINの表示
    Serial.println("onPassKeyNotify number");
    Serial.println(pass_key);
  }

  bool onSecurityRequest(){
    Serial.println("onSecurityRequest");
    return true;
  }

  void onAuthenticationComplete(esp_ble_auth_cmpl_t cmpl){
    Serial.println("onAuthenticationComplete");
    if(cmpl.success){
      // ペアリング完了
      Serial.println("auth success");
    }else{
      // ペアリング失敗
      Serial.println("auth failed");
    }
  }
};

//Timer割り込みイベントハンドラ
void IRAM_ATTR onTimer() {
  if(statusBlink==on){
    statusBlink=off;
  }else{
    statusBlink=on;
  }
}

void setLED(uint8_t colorPin,uint8_t turn){
  bool tmpChanged=false;
  switch (colorPin){
    case RED_LED:
      currentLedStatus.b.red=turn;
      if(currentLedStatus.b.red!=oldLedStatus.b.red){
        tmpChanged=true;
      }
      break;
    case GREEN_LED:
      currentLedStatus.b.green=turn;
      if(currentLedStatus.b.green!=oldLedStatus.b.green){
        tmpChanged=true;
      }
      break;
    case BLUE_LED:
      currentLedStatus.b.blue=turn;
      if(currentLedStatus.b.blue!=oldLedStatus.b.blue){
        tmpChanged=true;
      }
      break;
  }
  if(tmpChanged){
    if(IS_GPIO_POS){
      if(ledOnOff){
        if(turn==HIGH){
          digitalWrite(colorPin, HIGH);
        }else{
          digitalWrite(colorPin, LOW);
        }
      }
    }else{
      if(ledOnOff){
        if(turn==HIGH){
          digitalWrite(colorPin, LOW);
        }else{
          digitalWrite(colorPin, HIGH);
        }
      }
    }
  }
  oldLedStatus.UCHAR_8=currentLedStatus.UCHAR_8;
}
void clearLed(){
      setLED(RED_LED, LOW );
      setLED(GREEN_LED, LOW );
      setLED(BLUE_LED, LOW );
}

void soundBrink(){
  bool brink=true;
  if(stopSound!=stopSoundOld){
    for(int i=0;i<5;i++){
      if(brink){
        ledcWriteTone(1,C5);
        delay(BEAT);
        brink=false;
      }else{
        ledcWriteTone(1, 0);    // 音を止める
        brink=true;
        delay(BEAT);
      }
    }
    ledcWriteTone(1, 0);    // 音を止める
    stopSound=true;
  }
  stopSoundOld=stopSound;
}

void setup() {
  Serial.begin(115200);
  int time;                           // the variable used to set the Timer
  
  //BLEデバイスの作成
  BLEDevice::init(DEVICE_NAME);
  //  BLE Serverの作成
  pServer = BLEDevice::createServer();
  // BLE Serviceの作成
  BLEService *pService = pServer->createService(SERVICE_UUID);
  // BLE Characteristicの作成
  pCharacteristic = pService->createCharacteristic(
                      CHARACTERISTIC_UUID,
                      BLECharacteristic::PROPERTY_READ   |
                      BLECharacteristic::PROPERTY_WRITE  |
                      BLECharacteristic::PROPERTY_NOTIFY |
                      BLECharacteristic::PROPERTY_INDICATE
                    );
  //Serverコールバック関数の登録
  pServer->setCallbacks(new MyServerCallbacks());
  //Characteristicコールバック関数の登録
  pCharacteristic->setCallbacks(new MyCallbacks());
  
  //Pearing用コールバック関数の登録
  BLEDevice::setSecurityCallbacks(new MySecurity());   
  //Pearing設定
  BLESecurity *pSecurity = new BLESecurity();
  pSecurity->setKeySize(16);

  pSecurity->setAuthenticationMode(ESP_LE_AUTH_BOND);
  pSecurity->setCapability(ESP_IO_CAP_OUT);
  pSecurity->setInitEncryptionKey(ESP_BLE_ENC_KEY_MASK | ESP_BLE_ID_KEY_MASK);

  uint32_t passkey = BLE_PASSKEY;
  esp_ble_gap_set_security_param(ESP_BLE_SM_SET_STATIC_PASSKEY, &passkey, sizeof(uint32_t));
  
  // https://www.bluetooth.com/specifications/gatt/viewer?attributeXmlFile=org.bluetooth.descriptor.gatt.client_characteristic_configuration.xml
  // Create a BLE Descriptor
  pCharacteristic->addDescriptor(new BLE2902());

  // serviceの開始
  pService->start();
  // advertisingの取得
  BLEAdvertising *pAdvertising = BLEDevice::getAdvertising();
  pAdvertising->addServiceUUID(SERVICE_UUID);
  pAdvertising->setScanResponse(false);
  pAdvertising->setMinPreferred(0x0);  // set value to 0x00 to not advertise this parameter
  //advertisingの開始
  BLEDevice::startAdvertising();
  Serial.println("Waiting a client connection to notify...");
  // 本体初期化（UART有効, I2C無効, LED有効）
  M5.begin(false, false, true);
  // LED全消灯（赤, 緑, 青）
  M5.dis.drawpix(0, dispColor(0, 0, 0));

  //Timer割り込み設定
  tim0 = timerBegin(1, 80, true);
  timerAttachInterrupt(tim0, &onTimer, true);
  timerAlarmWrite(tim0, 1000000, true);
  timerAlarmEnable(tim0);

  //GPIO（LED)設定
  pinMode(RED_LED, OUTPUT);
  pinMode(GREEN_LED, OUTPUT);
  pinMode(BLUE_LED, OUTPUT);
  pinMode(BUTTON_LED_PIN, OUTPUT);
  
  //PIRピン設定
  //pinMode(PIR_PIN,INPUT);
  //Buttonピン設定
  pinMode(PUSH_BUTTON_PIN ,INPUT_PULLUP);
  
  //オープンドレインなので消灯はHIGH
  
  digitalWrite(RED_LED, HIGH);
  digitalWrite(GREEN_LED, HIGH);
  digitalWrite(BLUE_LED, HIGH);

  /*
  oldLedStatus.b.red=LOW;
  oldLedStatus.b.blue=LOW;
  oldLedStatus.b.green=LOW;
  setLED(RED_LED, LOW);
  setLED(GREEN_LED, LOW);
  setLED(BLUE_LED, LOW);
  */
  oldLedStatus.UCHAR_8=currentLedStatus.UCHAR_8;
  //圧電スピーカーセットアップ
  ledcSetup(1,12000, 8);
  ledcAttachPin(BUZZER_PIN,1);
}

void loop() {
  String strSend;//送信文字列
  clearLed();
  //LEDランプによるステータス表示
  switch(bleState){
    case connect://青色常時点灯
      M5.dis.drawpix(0, dispColor(0, 0, 255));
      setLED(BLUE_LED, HIGH);
      break;
    case disconnect://青色点滅
      switch(statusBlink){
        case on:
          M5.dis.drawpix(0, dispColor(0, 0, 255));
          setLED(BLUE_LED, IS_GPIO_POS ? HIGH : LOW );
          break;
        case off:
          M5.dis.drawpix(0, dispColor(0, 0, 0));
          setLED(BLUE_LED, IS_GPIO_POS ? LOW : HIGH  );
          break;
      }
      break;
    case ASAP:
      M5.dis.drawpix(0, dispColor(0, 0, 255));
      setLED(GREEN_LED, HIGH);
      soundBrink();
      break;
    case WAIT:
      stopSound=false;
      M5.dis.drawpix(0, dispColor(0, 0, 255));
      setLED(RED_LED, HIGH );
      setLED(GREEN_LED, HIGH);
      break;
    case WRONG:
      stopSound=false;
      M5.dis.drawpix(0, dispColor(0, 0, 255));
      setLED(RED_LED, HIGH);
      break;
  }
        //digitalWrite(BUTTON_LED_PIN, HIGH);
    // notify changed value
    if (deviceConnected) {
      if(digitalRead(PUSH_BUTTON_PIN)==HIGH ){
        digitalWrite(BUTTON_LED_PIN, HIGH);
        if(beforeStateSwitch==LOW){//スイッチがon→Offにてデータを送る
          Serial.println("PUSH_BUTTON OFF");
          strSend = "PUSH_OFF";
          pCharacteristic->setValue(strSend.c_str());
          pCharacteristic->notify();
          beforeStateSwitch=HIGH;
          ledcWriteTone(1,F4);
          delay(BEAT);
          ledcWriteTone(1, 0);    // 音を止める
        }
      }else{
        digitalWrite(BUTTON_LED_PIN, LOW);
        if(beforeStateSwitch==HIGH){//スイッチがOff→onにてデータを送る
          Serial.println("PUSH_BUTTON ON");
          strSend = "PUSH_ON";
          pCharacteristic->setValue(strSend.c_str());
          pCharacteristic->notify();
          beforeStateSwitch=LOW;

          ledcWriteTone(1,A4);
          delay(BEAT);
        }
      }
    }
    // disconnecting
    if (!deviceConnected && oldDeviceConnected) {
        delay(500); // give the bluetooth stack the chance to get things ready
        pServer->startAdvertising(); // restart advertising
        Serial.println("close connection and restart advertising");
        oldDeviceConnected = deviceConnected;
    }
    // connecting
    if (deviceConnected && !oldDeviceConnected) {
        // do stuff here on connecting
        oldDeviceConnected = deviceConnected;
    }
    M5.update();
    if (M5.Btn.wasPressed())
    {
      int key=BLE_PASSKEY;
      uint8_t* valueBleKey=(uint8_t*)(&key);
      
      Serial.println("PushButton!!");
      pCharacteristic->setValue(valueBleKey, 4);
      pCharacteristic->notify();
      //ledOnOff=!ledOnOff;
      if(bleState!=disconnect){
        bleState=connect;
      }
      /*
      if(IS_GPIO_POS)
      {
        digitalWrite(RED_LED, ((gpioLEDStatus % 3) > 0) ? LOW : HIGH );
        digitalWrite(GREEN_LED, (((gpioLEDStatus+1) % 3) > 0) ? LOW : HIGH );
        digitalWrite(BLUE_LED, (((gpioLEDStatus+2) % 3) > 0) ? LOW : HIGH );
      }else{
        digitalWrite(RED_LED, ((gpioLEDStatus % 3) > 0) ? HIGH : LOW);
        digitalWrite(GREEN_LED, (((gpioLEDStatus+1) % 3) > 0) ? HIGH : LOW);
        digitalWrite(BLUE_LED, (((gpioLEDStatus+2) % 3) > 0) ? HIGH : LOW);
      }
      gpioLEDStatus++;
      */
    }
    
  delay(20);
}
