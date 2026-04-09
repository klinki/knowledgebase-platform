import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';

import { AuthService } from './auth.service';

describe('AuthService', () => {
  let service: AuthService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([])
      ]
    });

    service = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);

    http.expectOne('http://localhost:5000/api/auth/me').flush(
      {
        id: 'user-1',
        email: 'member@example.com',
        displayName: 'Member',
        role: 'member',
        defaultLanguageCode: 'en',
        preservedLanguageCodes: ['fr']
      }
    );
  });

  afterEach(() => {
    http.verify();
    TestBed.resetTestingModule();
  });

  it('updates current user language preferences after saving preferences', async () => {
    await service.ensureSessionResolved();

    const savePromise = service.updatePreferences({
      defaultLanguageCode: 'de',
      preservedLanguageCodes: ['en']
    });

    const request = http.expectOne('http://localhost:5000/api/auth/preferences');
    expect(request.request.method).toBe('PUT');
    request.flush({
      defaultLanguageCode: 'de',
      preservedLanguageCodes: ['en'],
      supportedLanguages: [
        { code: 'de', displayName: 'German' },
        { code: 'en', displayName: 'English' }
      ]
    });

    await expect(savePromise).resolves.toEqual({
      defaultLanguageCode: 'de',
      preservedLanguageCodes: ['en'],
      supportedLanguages: [
        { code: 'de', displayName: 'German' },
        { code: 'en', displayName: 'English' }
      ]
    });

    expect(service.currentUser()?.defaultLanguageCode).toBe('de');
    expect(service.currentUser()?.preservedLanguageCodes).toEqual(['en']);
  });
});
