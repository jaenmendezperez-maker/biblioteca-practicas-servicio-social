import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Semestre } from '../../../core/models/semestre.model';
import { SemestreService } from '../../../core/services/semestre.service';
import { SidebarComponent } from '../../../shared/components/sidebar/sidebar.component';
import { SidebarService } from '../../../core/services/sidebar.service';

@Component({
  selector: 'app-lista-semestres',
  standalone: true,
  imports: [CommonModule, FormsModule, SidebarComponent],
  templateUrl: './lista-semestres.html'
})
export class ListaSemestresComponent implements OnInit {
  semestres: Semestre[] = [];
  cargando = true;
  textoBusqueda = '';

  constructor(
    private semestreService: SemestreService,
    private router: Router,
    public sidebarService: SidebarService
  ) {}

  ngOnInit(): void {
    this.semestreService.getSemestres().subscribe({
      next: (data) => {
        this.semestres = data;
        this.cargando = false;
      },
      error: () => (this.cargando = false)
    });
  }

  get semestresFiltrados(): Semestre[] {
    if (!this.textoBusqueda) return this.semestres;
    return this.semestres.filter((s) =>
      s.nombre.toLowerCase().includes(this.textoBusqueda.toLowerCase())
    );
  }

  irASemestre(id: number): void {
    this.router.navigate(['/'], { queryParams: { semestre: id } });
  }
}