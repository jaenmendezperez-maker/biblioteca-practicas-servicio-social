import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SubirLibro } from './subir-libro';

describe('SubirLibro', () => {
  let component: SubirLibro;
  let fixture: ComponentFixture<SubirLibro>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SubirLibro]
    })
    .compileComponents();

    fixture = TestBed.createComponent(SubirLibro);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
