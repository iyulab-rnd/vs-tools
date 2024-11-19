// src/binaryDetector.ts
import * as fs from 'fs';

export class BinaryDetector {
  private static readonly BUFFER_SIZE = 8192;  // 8KB
  private static readonly NULL_PERCENTAGE_THRESHOLD = 0.1;  // 10%
  private static readonly CONTROL_CHAR_THRESHOLD = 0.1;    // 10%

  private static readonly ALLOWED_CONTROL_CHARS = new Set([
    9,    // 탭
    10,   // LF (새줄)
    13,   // CR (캐리지 리턴)
    12    // FF (폼 피드)
  ]);

  private static mimeCache = new Map<string, boolean>();

  /**
   * 파일이 바이너리인지 검사
   */
  static isFileBinary(filePath: string): boolean {
    // 캐시된 결과 확인
    const cached = this.mimeCache.get(filePath);
    if (cached !== undefined) {
      return cached;
    }

    let result = false;

    try {
      // 파일의 처음 8KB를 읽어서 바이너리 여부 확인
      const fd = fs.openSync(filePath, 'r');
      const buffer = Buffer.alloc(this.BUFFER_SIZE);
      const bytesRead = fs.readSync(fd, buffer, 0, this.BUFFER_SIZE, 0);
      fs.closeSync(fd);

      // 실제로 읽은 부분만 검사
      const slice = buffer.slice(0, bytesRead);
      result = this.containsBinaryContent(slice);

      // 결과 캐시
      this.updateCache(filePath, result);
      
    } catch (error) {
      console.error(`Error checking binary content for ${filePath}:`, error);
      // 읽기 오류 발생 시 안전하게 바이너리로 처리
      result = true;
    }

    return result;
  }

  /**
   * 버퍼의 내용이 바이너리인지 검사
   */
  private static containsBinaryContent(buffer: Buffer): boolean {
    let nullCount = 0;
    let controlCount = 0;
    const totalBytes = buffer.length;

    for (let i = 0; i < totalBytes; i++) {
      const byte = buffer[i];
      
      // NULL 바이트 검사
      if (byte === 0) {
        nullCount++;
        if (nullCount / totalBytes > this.NULL_PERCENTAGE_THRESHOLD) {
          return true;
        }
      }
      
      // 허용되지 않는 제어 문자 검사
      if (byte < 32 && !this.ALLOWED_CONTROL_CHARS.has(byte)) {
        controlCount++;
        if (controlCount / totalBytes > this.CONTROL_CHAR_THRESHOLD) {
          return true;
        }
      }
      
      // UTF-8 이진수 체크 (잘못된 UTF-8 시퀀스)
      if ((byte & 0xF8) === 0xF8) {
        return true;
      }
    }

    return false;
  }

  /**
   * 캐시 업데이트 및 관리
   */
  private static updateCache(filePath: string, result: boolean): void {
    this.mimeCache.set(filePath, result);
    
    // 캐시 크기 제한 (1000개 초과시 200개 제거)
    if (this.mimeCache.size > 1000) {
      const keys = Array.from(this.mimeCache.keys());
      for (let i = 0; i < 200; i++) {
        this.mimeCache.delete(keys[i]);
      }
    }
  }

  /**
   * 캐시 초기화
   */
  static clearCache(): void {
    this.mimeCache.clear();
  }
}