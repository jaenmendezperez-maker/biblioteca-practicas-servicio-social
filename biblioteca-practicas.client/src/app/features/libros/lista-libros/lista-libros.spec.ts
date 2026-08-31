import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ListaLibros } from './lista-libros';

describe('ListaLibros', () => {
  let component: ListaLibros;
  let fixture: ComponentFixture<ListaLibros>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ListaLibros]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ListaLibros);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
